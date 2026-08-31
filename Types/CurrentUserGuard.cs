using Updraft.Security;
using Updraft.Services;

namespace Updraft.Types;

internal static class CurrentUserGuard
{
    // Resolvers require a registered user; an authenticated-but-unregistered caller is rejected.
    public static CurrentUser OrThrow(this CurrentUser? user) => user ?? throw new UnknownUserException();
}
