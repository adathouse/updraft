namespace Updraft.Security;

public static class RoleNames
{
    public const string Requester = "Requester";
    public const string Drafter = "Drafter";
    public const string FrontOffice = "FrontOffice";
}

public static class AuthorizationPolicies
{
    public const string Requester = nameof(Requester);
    public const string Drafter = nameof(Drafter);
    public const string FrontOffice = nameof(FrontOffice);
    public const string DrafterOrFrontOffice = nameof(DrafterOrFrontOffice);
    public const string RequesterOrFrontOffice = nameof(RequesterOrFrontOffice);
    public const string DrafterOrRequester = nameof(DrafterOrRequester);
    public const string AnyKnownRole = nameof(AnyKnownRole);
}