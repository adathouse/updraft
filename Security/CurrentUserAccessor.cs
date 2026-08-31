using System.Security.Claims;
using Updraft.Data.Entities;
using Updraft.Repositories;
using Updraft.Services;

namespace Updraft.Security;

public sealed class CurrentUserAccessor(
    IHttpContextAccessor httpContextAccessor,
    IUserRepository userRepository) : ICurrentUserAccessor
{
    private CurrentUser? _cached;
    private bool _resolved;

    public async ValueTask<CurrentUser> GetRequiredAsync(CancellationToken cancellationToken) =>
        await TryGetAsync(cancellationToken) ?? throw new UnknownUserException();

    public async ValueTask<CurrentUser?> TryGetAsync(CancellationToken cancellationToken)
    {
        if (_resolved)
        {
            return _cached;
        }

        PrincipalIdentity identity = GetPrincipalIdentity();
        User? user = await userRepository.GetByEntraIdAsync(identity.EntraId, cancellationToken);

        _cached = user is null
            ? null
            : new CurrentUser(
                user.UserId,
                user.EntraId,
                identity.Roles.Contains(RoleNames.Requester),
                identity.Roles.Contains(RoleNames.Drafter),
                identity.Roles.Contains(RoleNames.FrontOffice));
        _resolved = true;
        return _cached;
    }

    public PrincipalIdentity GetPrincipalIdentity()
    {
        ClaimsPrincipal? principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity is null || !principal.Identity.IsAuthenticated)
        {
            throw new InvalidOperationException("No authenticated principal is available for the current request.");
        }

        // JwtBearer maps the token `sub` claim to NameIdentifier; that value is the (possibly synthetic) Entra id.
        string? entraId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(entraId))
        {
            throw new InvalidOperationException("The authenticated principal does not carry an identity claim.");
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
