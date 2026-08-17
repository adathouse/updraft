using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Updraft.Data;
using Updraft.Repositories;
using Updraft.Security;
using Updraft.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.RequireHttpsMetadata = false;
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = false,
			ValidateAudience = false,
			ValidateIssuerSigningKey = false,
			ValidateLifetime = true
		};
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

builder.Services.AddScoped<IAttachmentRepository, AttachmentRepository>();
builder.Services.AddScoped<ICommitteeRepository, CommitteeRepository>();
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

builder.Services
	.AddGraphQLServer()
	.AddAuthorizationCore()
	.AddFiltering()
	.AddSorting()
	.AddProjections()
	.AddQueryConventions()
	.AddGlobalObjectIdentification()
	.AddUpdraftTypes();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok("Updraft GraphQL API"));
app.MapGraphQL("/graphql");

app.Run();
