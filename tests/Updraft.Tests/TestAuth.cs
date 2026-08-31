using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Updraft.Tests;

// Shared JWT settings used by both the test host (to validate) and TestTokens (to sign).
internal static class TestAuth
{
    public const string Issuer = "dotnet-user-jwts";
    public const string Audience = "http://0.0.0.0:5048";

    // Symmetric HS256 key; base64 of these bytes is injected into the host's Bearer config.
    public static readonly byte[] SigningKeyBytes =
        Encoding.UTF8.GetBytes("updraft-integration-tests-symmetric-signing-key-0123456789");

    public static readonly SymmetricSecurityKey SigningKey = new(SigningKeyBytes);
}
