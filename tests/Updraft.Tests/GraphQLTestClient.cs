using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Updraft.Tests;

// Thin GraphQL-over-HTTP helper for scenario tests: posts to /graphql and exposes data + errors.
internal sealed class GraphQLTestClient(HttpClient client)
{
    public async Task<GraphQLResult> ExecuteAsync(
        string query,
        object? variables,
        string? token,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/graphql")
        {
            Content = JsonContent.Create(new { query, variables }),
        };

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return GraphQLResult.Parse(response.StatusCode, body);
    }
}

// A parsed GraphQL response; Data is cloned so it stays valid after the source document is disposed.
internal sealed record GraphQLResult(
    HttpStatusCode StatusCode,
    JsonElement Data,
    IReadOnlyList<string> Errors,
    string RawBody)
{
    public static GraphQLResult Parse(HttpStatusCode statusCode, string body)
    {
        using var document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;

        JsonElement data = root.TryGetProperty("data", out var dataElement)
            && dataElement.ValueKind != JsonValueKind.Null
            ? dataElement.Clone()
            : default;

        var errors = new List<string>();
        if (root.TryGetProperty("errors", out var errorArray) && errorArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var error in errorArray.EnumerateArray())
            {
                string message = error.TryGetProperty("message", out var messageElement)
                    && messageElement.ValueKind == JsonValueKind.String
                        ? messageElement.GetString()!
                        : "(no message)";
                string? code = error.TryGetProperty("extensions", out var extensions)
                    && extensions.TryGetProperty("code", out var codeElement)
                    && codeElement.ValueKind == JsonValueKind.String
                        ? codeElement.GetString()
                        : null;
                errors.Add(code is null ? message : $"{code}: {message}");
            }
        }

        return new GraphQLResult(statusCode, data, errors, body);
    }

    // Returns the data payload, failing when the response was not a clean 200 with no transport-level errors.
    public JsonElement EnsureData()
    {
        if (StatusCode != HttpStatusCode.OK)
        {
            throw new InvalidOperationException($"GraphQL request failed with HTTP {(int)StatusCode}. Body: {RawBody}");
        }

        if (Errors.Count > 0)
        {
            throw new InvalidOperationException($"GraphQL response contained errors: {string.Join("; ", Errors)}");
        }

        if (Data.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException($"GraphQL response contained no data. Body: {RawBody}");
        }

        return Data;
    }

    // Navigates nested object properties from the data root, e.g. Select("submitRequest", "request", "id").
    public JsonElement Select(params string[] path)
    {
        JsonElement current = EnsureData();
        foreach (var segment in path)
        {
            current = current.GetProperty(segment);
        }

        return current;
    }
}
