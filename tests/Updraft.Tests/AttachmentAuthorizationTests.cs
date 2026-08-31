using System.Net;
using System.Net.Http.Headers;
using Updraft.Security;
using Xunit;

namespace Updraft.Tests;

public sealed class AttachmentAuthorizationTests : IClassFixture<UpdraftWebApplicationFactory>
{
    private static readonly string SamplePdf = TestDataPath("H2821_RH_xml.pdf");

    public static TheoryData<string> SampleFiles =>
    [
        "H2821_RH_xml.pdf",
        "H4348_RH_xml.pdf",
    ];

    private readonly UpdraftWebApplicationFactory _factory;

    public AttachmentAuthorizationTests(UpdraftWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Upload_WithoutToken_IsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await UploadAsync(client, SamplePdf, token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_WithUnknownRole_IsForbidden()
    {
        var client = _factory.CreateClient();

        // Valid token, but the role is outside AnyKnownRole -> authenticated yet not authorized.
        var response = await UploadAsync(client, SamplePdf, TestTokens.Mint("Nobody"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SampleFiles))]
    public async Task Upload_WithKnownRole_PassesAuthorization(string fileName)
    {
        var client = _factory.CreateClient();

        var response = await UploadAsync(client, TestDataPath(fileName), TestTokens.Mint(RoleNames.Requester));

        // Auth passed if neither 401 nor 403; the attachment record may not exist (out of scope).
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Resolves a file vendored under TestData/, copied next to the test assembly at build time.
    private static string TestDataPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

    private static async Task<HttpResponseMessage> UploadAsync(HttpClient client, string filePath, string? token)
    {
        var bytes = await File.ReadAllBytesAsync(filePath, TestContext.Current.CancellationToken);
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/attachments/{Guid.NewGuid()}/{Path.GetFileName(filePath)}")
        {
            Content = content,
        };

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
