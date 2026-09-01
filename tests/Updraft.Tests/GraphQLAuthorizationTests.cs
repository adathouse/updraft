using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Updraft.Security;
using Xunit;

namespace Updraft.Tests;

public sealed class GraphQLAuthorizationTests : IClassFixture<UpdraftWebApplicationFactory>
{
    // FrontOffice-only query; a good probe for both authn and role enforcement.
    private const string RequestsQuery = "{ requests { nodes { id } } }";

    private readonly UpdraftWebApplicationFactory _factory;

    public GraphQLAuthorizationTests(UpdraftWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Requests_WithoutToken_IsNotAuthenticated()
    {
        var client = _factory.CreateClient();

        var response = await SendAsync(client, RequestsQuery, token: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AUTH_NOT_AUTHENTICATED", await ErrorCodesAsync(response));
    }

    [Fact]
    public async Task Requests_WithWrongRole_IsNotAuthorized()
    {
        var client = _factory.CreateClient();

        var response = await SendAsync(client, RequestsQuery, TestTokens.Mint(RoleNames.Drafter));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AUTH_NOT_AUTHORIZED", await ErrorCodesAsync(response));
    }

    [Fact]
    public async Task Requests_WithFrontOfficeRole_PassesAuthorization()
    {
        var client = _factory.CreateClient();

        var response = await SendAsync(client, RequestsQuery, TestTokens.Mint(RoleNames.FrontOffice));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Auth passed if neither error is present; underlying data/DB state is irrelevant here.
        var codes = await ErrorCodesAsync(response);
        Assert.DoesNotContain("AUTH_NOT_AUTHENTICATED", codes);
        Assert.DoesNotContain("AUTH_NOT_AUTHORIZED", codes);
    }

    [Fact]
    public async Task Requests_WithMalformedToken_IsNotAuthenticated()
    {
        var client = _factory.CreateClient();

        var response = await SendAsync(client, RequestsQuery, "not-a-real-jwt");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("AUTH_NOT_AUTHENTICATED", await ErrorCodesAsync(response));
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string query, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = JsonContent.Create(new { query }),
        };

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<IReadOnlyList<string>> ErrorCodesAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("errors", out var errors)
            || errors.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var codes = new List<string>();
        foreach (var error in errors.EnumerateArray())
        {
            if (error.TryGetProperty("extensions", out var extensions)
                && extensions.TryGetProperty("code", out var code)
                && code.ValueKind == JsonValueKind.String)
            {
                codes.Add(code.GetString()!);
            }
        }

        return codes;
    }
}
