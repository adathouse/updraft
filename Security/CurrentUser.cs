using System.Security.Claims;

namespace Updraft.Security;

// The registered user backing the current request, resolved from the authenticated principal.
public sealed record CurrentUser(
    Guid UserId,
    string EntraId,
    bool IsRequester,
    bool IsDrafter,
    bool IsFrontOffice);

// The raw identity read from the authenticated principal, used to look up or register a user.
public sealed record PrincipalIdentity(
    string EntraId,
    string Name,
    string Email,
    IReadOnlyList<string> Roles)
{
    // Reads identity claims from an authenticated principal; null when no stable identity claim is present.
    public static PrincipalIdentity? FromPrincipal(ClaimsPrincipal principal)
    {
        if (principal.Identity is null || !principal.Identity.IsAuthenticated)
        {
            return null;
        }

        // JwtBearer maps the token `sub` claim to NameIdentifier; that value is the (possibly synthetic) Entra id.
        string? entraId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(entraId))
        {
            return null;
        }

        string name = principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("name")
            ?? string.Empty;
        string email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email")
            ?? string.Empty;
        IReadOnlyList<string> roles = [.. principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value)];

        return new PrincipalIdentity(entraId, name, email, roles);
    }
}
