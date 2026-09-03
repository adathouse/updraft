using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Updraft.Tests;

internal static class TestTokens
{
    // Mints a Bearer token signed with the test key. Roles are emitted as "role" claims,
    // which the host maps to ClaimTypes.Role for RequireRole policy checks.
    public static string Mint(params string[] roles) =>
        MintFor("test-user", "test-user@example.com", roles);

    // Mints a token for a distinct identity so scenario tests can register separate users.
    // `subject` becomes the stable EntraId (sub) and display name (unique_name), mirroring `dotnet user-jwts --name`.
    public static string MintFor(string subject, string email, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(JwtRegisteredClaimNames.UniqueName, subject),
            new(JwtRegisteredClaimNames.Email, email),
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var credentials = new SigningCredentials(TestAuth.SigningKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: TestAuth.Issuer,
            audience: TestAuth.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
