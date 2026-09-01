using HotChocolate;

namespace Updraft.Security;

// Injects the request's resolved CurrentUser (may be null when unauthenticated or unregistered).
public sealed class CurrentUserAttribute() : GlobalStateAttribute(StateKey)
{
    public const string StateKey = "CurrentUser";
}
