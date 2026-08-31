namespace Updraft.Security;

public interface ICurrentUserAccessor
{
    // Resolves the registered user for the current request; throws when they are not registered.
    ValueTask<CurrentUser> GetRequiredAsync(CancellationToken cancellationToken);

    // Resolves the registered user for the current request, or null when they are not registered.
    ValueTask<CurrentUser?> TryGetAsync(CancellationToken cancellationToken);

    // Reads identity claims from the authenticated principal without requiring a user row.
    PrincipalIdentity GetPrincipalIdentity();
}
