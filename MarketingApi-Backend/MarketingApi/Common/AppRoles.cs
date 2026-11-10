namespace MarketingApi.Common;

/// <summary>
/// Canonical application role names and authorization policy names.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string SupervisorMaster = "Supervisor Master";
    public const string LegacySuperviserMaster = "Superviser Master";
    public const string MarketingMaster = "Marketing Master";
    public const string DefaultUser = "User";

    public const string SalesmanPolicy = "SalesmanAccess";
    public const string MarketingPolicy = "MarketingAccess";
    public const string AnyMasterPolicy = "AnyMasterAccess";

    /// <summary>
    /// Normalizes legacy role names (such as "Superviser Master") into canonical role names.
    /// </summary>
    public static string Normalize(string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return DefaultUser;
        }

        var trimmed = roleName.Trim();
        if (string.Equals(trimmed, LegacySuperviserMaster, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, SupervisorMaster, StringComparison.OrdinalIgnoreCase))
        {
            return SupervisorMaster;
        }

        if (string.Equals(trimmed, MarketingMaster, StringComparison.OrdinalIgnoreCase))
        {
            return MarketingMaster;
        }

        if (string.Equals(trimmed, Admin, StringComparison.OrdinalIgnoreCase))
        {
            return Admin;
        }

        return trimmed;
    }
}
