using System.Security.Claims;
using Updraft.Data.Entities;
using Updraft.Repositories;

namespace Updraft.Security;

public interface ICurrentUserResolver
{
    // Resolves the registered user for a principal, or null when unauthenticated or unregistered.
    Task<CurrentUser?> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}

public sealed class CurrentUserResolver(IUserRepository userRepository) : ICurrentUserResolver
{
    public async Task<CurrentUser?> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        PrincipalIdentity? identity = PrincipalIdentity.FromPrincipal(principal);
        if (identity is null)
        {
            return null;
        }

        User? user = await userRepository.GetByEntraIdAsync(identity.EntraId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return new CurrentUser(
            user.UserId,
            user.EntraId,
            identity.Roles.Contains(RoleNames.Requester),
            identity.Roles.Contains(RoleNames.Drafter),
            identity.Roles.Contains(RoleNames.FrontOffice));
    }
}
