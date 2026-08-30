using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Foundatio.Storage;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Updraft.Data;
using Updraft.Repositories;
using Updraft.Security;
using Updraft.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		// Local dev tokens are minted with `dotnet user-jwts`, which writes the issuer,
		// audiences, and signing key into configuration (Authentication:Schemes:Bearer)
		// plus user secrets. The JwtBearer handler binds that config automatically, so we
		// only enable validation here and leave https off for local http.
		options.RequireHttpsMetadata = false;
		options.TokenValidationParameters.ValidateIssuer = true;
		options.TokenValidationParameters.ValidateAudience = true;
		options.TokenValidationParameters.ValidateIssuerSigningKey = true;
		options.TokenValidationParameters.ValidateLifetime = true;
		// user-jwts emits roles in the "role" claim; RequireRole matches ClaimTypes.Role.
		options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;

		// JwtBearer swallows validation failures into a 401, so surface them on the request span.
		options.Events = new JwtBearerEvents
		{
			OnAuthenticationFailed = context =>
			{
				var activity = Activity.Current;
				activity?.AddException(context.Exception);
				activity?.SetStatus(ActivityStatusCode.Error, context.Exception.Message);
				return Task.CompletedTask;
			}
		};

		// TODO (Entra): outside Development, validate against the Updraft - DEV app
		// registration instead of the user-jwts dev key. Entra app roles arrive in the
		// "roles" claim, so RoleClaimType must change to match.
		//   tenant:    4979d838-afe7-4f16-ac52-461bafc329ae
		//   client id: 4d67f493-8e21-46ec-825a-afed3b38e9e5
		//   audience:  api://4d67f493-8e21-46ec-825a-afed3b38e9e5
		// if (!builder.Environment.IsDevelopment())
		// {
		//     options.Authority = "https://login.microsoftonline.com/4979d838-afe7-4f16-ac52-461bafc329ae/v2.0";
		//     options.Audience = "api://4d67f493-8e21-46ec-825a-afed3b38e9e5";
		//     options.RequireHttpsMetadata = true;
		//     options.TokenValidationParameters.RoleClaimType = "roles";
		// }
	});

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy(AuthorizationPolicies.Requester, policy => policy.RequireRole(RoleNames.Requester));
	options.AddPolicy(AuthorizationPolicies.Drafter, policy => policy.RequireRole(RoleNames.Drafter));
	options.AddPolicy(AuthorizationPolicies.FrontOffice, policy => policy.RequireRole(RoleNames.FrontOffice));
	options.AddPolicy(
		AuthorizationPolicies.DrafterOrFrontOffice,
		policy => policy.RequireRole(RoleNames.Drafter, RoleNames.FrontOffice));
	options.AddPolicy(
		AuthorizationPolicies.AnyKnownRole,
		policy => policy.RequireRole(RoleNames.Requester, RoleNames.Drafter, RoleNames.FrontOffice));
});

var connectionString = builder.Configuration.GetConnectionString("Updraft")
	?? "Host=db;Port=5432;Database=updraft;Username=updraft;Password=updraft";

builder.Services.AddDbContext<UpdraftDbContext>(options => options.UseNpgsql(connectionString));

if (builder.Environment.IsDevelopment())
{
	var storageFolder = builder.Configuration["Storage:LocalPath"] ?? "storage";
	builder.Services.AddSingleton<IFileStorage>(_ =>
		new FolderFileStorage(new FolderFileStorageOptions { Folder = storageFolder }));
}

builder.Services.AddScoped<IAttachmentRepository, AttachmentRepository>();
builder.Services.AddScoped<IDraftRepository, DraftRepository>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<IOfficeRepository, OfficeRepository>();
builder.Services.AddScoped<IRequestRepository, RequestRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<DraftService>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<NoteService>();
builder.Services.AddScoped<RequestService>();
builder.Services.AddScoped<AttachmentService>();

// Endpoint is configured via OTEL_EXPORTER_OTLP_PROTOCOL and OTEL_EXPORTER_OTLP_ENDPOINT.
builder.Logging.AddOpenTelemetry(logging =>
{
	logging.IncludeFormattedMessage = true;
	logging.IncludeScopes = true;
});

builder.Services
	.AddOpenTelemetry()
	.ConfigureResource(resource => resource.AddService("Updraft"))
	.WithTracing(tracing => tracing
		.AddAspNetCoreInstrumentation(o => o.RecordException = true)
		.AddHttpClientInstrumentation()
		.AddHotChocolateInstrumentation())
	.WithMetrics(metrics => metrics
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation())
	.UseOtlpExporter();

builder.Services
	.AddGraphQLServer()
	.AddInstrumentation()
	.AddAuthorization()
	.AddFiltering()
	.AddSorting()
	.AddProjections()
	.AddQueryConventions()
	.AddMutationConventions()
	.AddGlobalObjectIdentification()
	.AddUpdraftTypes();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok("Updraft GraphQL API"));
app.MapGraphQL("/graphql");

app.MapPost("/attachments/{attachmentId}/{fileName}", async (
	HttpRequest request,
	Guid attachmentId,
	string fileName,
	AttachmentService attachmentService,
	CancellationToken cancellationToken) =>
{
	var contentType = request.ContentType ?? "application/octet-stream";
	var command = new AttachDocumentCommand(attachmentId, request.Body, fileName, contentType);
	var attachment = await attachmentService.AttachDocumentAsync(command, cancellationToken);
	return Results.Ok(attachment);
}).RequireAuthorization(AuthorizationPolicies.AnyKnownRole);

// TODO: add a fetch endpoint. The GET should follow the attachment.Uri value

app.Run();
