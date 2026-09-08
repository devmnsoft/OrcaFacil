namespace OrcaFacil.Application.Accounts.Admin;

public static class ProfilePermissionMatrix
{
    public static readonly IReadOnlySet<string> GlobalProfiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "SuperAdministrator", "SuperAdmin", "GlobalSupport", "GlobalBilling", "GlobalAuditor", "PlatformSupport", "PlatformFinance", "PlatformAuditor" };
    public static readonly IReadOnlySet<string> AccountProfiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "Owner", "Administrator", "Manager", "Commercial", "Operations", "Technician", "Financial", "Fiscal", "CustomerSuccess", "Support", "ReadOnly", "ClientPortalUser", "PartnerPortalUser" };

    public static bool CanAssign(string actorProfile, string requestedProfile) =>
        AccountProfiles.Contains(requestedProfile) && (actorProfile == "Owner" || actorProfile == "Administrator" && requestedProfile != "Owner");
}
