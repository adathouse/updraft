using System.Security.Claims;
using Updraft.Data.Entities;
using Updraft.Repositories;
using Updraft.Security;

namespace Updraft.Services;

public sealed class UserService(IUserRepository userRepository)
{
    // Ties the authenticated principal to a users row, creating one on first sign-in.
    public async Task<User> RegisterCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        PrincipalIdentity identity = PrincipalIdentity.FromPrincipal(principal)
            ?? throw new InvalidOperationException("The authenticated principal does not carry an identity claim.");

        User? existing = await userRepository.GetByEntraIdAsync(identity.EntraId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),
            EntraId = identity.EntraId,
            Name = identity.Name,
            Email = identity.Email,
            Roles = string.Join(',', identity.Roles),
        };

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
        return user;
    }
}
