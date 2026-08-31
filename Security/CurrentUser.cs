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
    IReadOnlyList<string> Roles);
