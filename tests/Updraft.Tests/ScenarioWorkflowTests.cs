using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Updraft.Security;
using Xunit;

namespace Updraft.Tests;

// End-to-end happy path from scenario.md: request -> job -> draft -> attachment -> upload.
// Requires a reachable PostgreSQL with Flyway-seeded offices (see README "Running the tests").
public sealed class ScenarioWorkflowTests : IClassFixture<UpdraftWebApplicationFactory>
{
    private const string RegisterMutation =
        "mutation { registerCurrentUser { user { id } } }";

    private const string OfficesQuery =
        "query { offices(first: 50) { nodes { id name } } }";

    private const string SubmitRequestMutation = """
        mutation SubmitRequest($input: SubmitRequestInput!) {
          submitRequest(input: $input) {
            request { id status }
            errors { __typename ... on Error { message } }
          }
        }
        """;

    private const string CreateJobMutation = """
        mutation CreateJob($input: CreateJobInput!) {
          createJob(input: $input) {
            job { id status }
            errors { __typename ... on Error { message } }
          }
        }
        """;

    private const string SubmitDraftMutation = """
        mutation SubmitDraft($input: SubmitDraftInput!) {
          submitDraft(input: $input) {
            draft { id comment }
            errors { __typename ... on Error { message } }
          }
        }
        """;

    private const string AddAttachmentMutation = """
        mutation AddDraftAttachment($input: AddAttachmentInput!) {
          addAttachment(input: $input) {
            attachment { id attachmentUri attachmentRole }
            errors { __typename ... on Error { message } }
          }
        }
        """;

    private readonly UpdraftWebApplicationFactory _factory;

    public ScenarioWorkflowTests(UpdraftWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task FullDraftingWorkflow_Should_CreateJobDraftAndUploadAttachment_When_RolesActInSequence()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        HttpClient http = _factory.CreateClient();
        var graphql = new GraphQLTestClient(http);

        // Stable per-role identities so registerCurrentUser is idempotent across repeated runs.
        string requesterToken = TestTokens.MintFor("scenario-requester", "scenario-requester@example.com", RoleNames.Requester);
        string frontOfficeToken = TestTokens.MintFor("scenario-frontoffice", "scenario-frontoffice@example.com", RoleNames.FrontOffice);
        string drafterToken = TestTokens.MintFor("scenario-drafter", "scenario-drafter@example.com", RoleNames.Drafter);

        // Every authorized call needs a registered users row; the drafter's global id is the job assignee.
        await RegisterUserAsync(graphql, requesterToken, cancellationToken);
        await RegisterUserAsync(graphql, frontOfficeToken, cancellationToken);
        string drafterId = await RegisterUserAsync(graphql, drafterToken, cancellationToken);

        string officeId = await FirstOfficeIdAsync(graphql, requesterToken, cancellationToken);

        // 1. Requester submits a new (Unassigned) request.
        GraphQLResult submitRequest = await graphql.ExecuteAsync(
            SubmitRequestMutation,
            new
            {
                input = new
                {
                    officeId,
                    proposal = "Smart Samples for Sampling",
                    scopeResponse = "Restricts the sampling of samples.",
                    administrationResponse = "Department of Samples",
                    enforcementResponse = "Civil penalties",
                    timingResponse = "Effective January 4, 2027",
                    existingLawResponse = "The Sample Act of 1932",
                    committeeIds = Array.Empty<string>(),
                    tagIds = Array.Empty<string>(),
                },
            },
            requesterToken,
            cancellationToken);
        AssertNoPayloadErrors(submitRequest, "submitRequest");
        string requestId = submitRequest.Select("submitRequest", "request", "id").GetString()!;
        Assert.False(string.IsNullOrEmpty(requestId));
        Assert.Equal("UNASSIGNED", submitRequest.Select("submitRequest", "request", "status").GetString());

        // 2. FrontOffice creates the job and assigns it to the drafter.
        GraphQLResult createJob = await graphql.ExecuteAsync(
            CreateJobMutation,
            new
            {
                input = new
                {
                    requestId,
                    assigneeId = drafterId,
                    description = "Sample draft of the sampling bill.",
                },
            },
            frontOfficeToken,
            cancellationToken);
        AssertNoPayloadErrors(createJob, "createJob");
        string jobId = createJob.Select("createJob", "job", "id").GetString()!;
        Assert.False(string.IsNullOrEmpty(jobId));
        Assert.Equal("OPEN", createJob.Select("createJob", "job", "status").GetString());

        // 3. Assigned drafter submits a draft.
        GraphQLResult submitDraft = await graphql.ExecuteAsync(
            SubmitDraftMutation,
            new { input = new { jobId, comment = "Sample draft" } },
            drafterToken,
            cancellationToken);
        AssertNoPayloadErrors(submitDraft, "submitDraft");
        string draftId = submitDraft.Select("submitDraft", "draft", "id").GetString()!;
        Assert.False(string.IsNullOrEmpty(draftId));
        Assert.Equal("Sample draft", submitDraft.Select("submitDraft", "draft", "comment").GetString());

        // 4. Drafter creates the draft attachment record.
        GraphQLResult addAttachment = await graphql.ExecuteAsync(
            AddAttachmentMutation,
            new { input = new { role = "DRAFT", draftId } },
            drafterToken,
            cancellationToken);
        AssertNoPayloadErrors(addAttachment, "addAttachment");
        Assert.Equal("DRAFT", addAttachment.Select("addAttachment", "attachment", "attachmentRole").GetString());
        string attachmentUri = addAttachment.Select("addAttachment", "attachment", "attachmentUri").GetString()!;

        // The upload guid is the third path segment: draft/{draftGuid}/{attachmentGuid}.
        string[] uriSegments = attachmentUri.Split('/');
        Assert.Equal(3, uriSegments.Length);
        Assert.Equal("draft", uriSegments[0]);
        Guid uploadGuid = Guid.Parse(uriSegments[2]);

        // 5. Drafter uploads the file bytes; the endpoint returns the persisted storage key.
        const string fileName = "H4348_RH_xml.pdf";
        HttpResponseMessage upload = await UploadAsync(http, uploadGuid, TestDataPath(fileName), drafterToken, cancellationToken);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        string body = await upload.Content.ReadAsStringAsync(cancellationToken);
        string? storageKey = JsonSerializer.Deserialize<string>(body);
        Assert.Equal($"{attachmentUri}/{fileName}", storageKey);
    }

    private static async Task<string> RegisterUserAsync(GraphQLTestClient graphql, string token, CancellationToken cancellationToken)
    {
        GraphQLResult result = await graphql.ExecuteAsync(RegisterMutation, variables: null, token, cancellationToken);
        return result.Select("registerCurrentUser", "user", "id").GetString()!;
    }

    private static async Task<string> FirstOfficeIdAsync(GraphQLTestClient graphql, string token, CancellationToken cancellationToken)
    {
        GraphQLResult result = await graphql.ExecuteAsync(OfficesQuery, variables: null, token, cancellationToken);
        JsonElement nodes = result.Select("offices", "nodes");
        Assert.True(nodes.GetArrayLength() > 0, "Expected Flyway-seeded offices; is the database migrated?");
        return nodes[0].GetProperty("id").GetString()!;
    }

    private static void AssertNoPayloadErrors(GraphQLResult result, string mutationField)
    {
        JsonElement payload = result.Select(mutationField);
        if (payload.TryGetProperty("errors", out JsonElement errors)
            && errors.ValueKind == JsonValueKind.Array
            && errors.GetArrayLength() > 0)
        {
            IEnumerable<string?> messages = errors.EnumerateArray().Select(error =>
                error.TryGetProperty("message", out JsonElement message) ? message.GetString() : error.GetRawText());
            Assert.Fail($"{mutationField} returned payload errors: {string.Join("; ", messages)}");
        }
    }

    // Resolves a file vendored under TestData/, copied next to the test assembly at build time.
    private static string TestDataPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        Guid attachmentId,
        string filePath,
        string token,
        CancellationToken cancellationToken)
    {
        byte[] bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/attachments/{attachmentId}/{Path.GetFileName(filePath)}")
        {
            Content = content,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await client.SendAsync(request, cancellationToken);
    }
}
