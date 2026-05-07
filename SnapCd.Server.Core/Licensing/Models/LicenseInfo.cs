namespace SnapCd.Server.Core.Licensing.Models;

/// <summary>
/// The licensing tier carried in the license JWT. Determines which features are
/// available. The number of modules included is carried separately in the
/// <see cref="LicenseInfo.MaxModules"/> field and is independent of the tier
/// (e.g. CommunityPlus and Lite both ship with 20 modules but different feature sets).
/// </summary>
public enum Tier
{
    Community,
    Lite,
    Enterprise
}

public class LicenseInfo
{
    public Tier Tier { get; set; }
    public bool IsValid { get; set; }

    /// <summary>
    /// Maximum number of modules. <c>null</c> means unlimited.
    /// </summary>
    public int? MaxModules { get; set; }

    public DateTime? ExpiryDate { get; set; }
    public DateTime? LicensePeriodEnd { get; set; }
    public Guid? SubscriptionId { get; set; }
    public string? ValidationError { get; set; }

    /// <summary>
    /// Default no-license state: Community tier, 10-module cap, IsValid=false.
    /// Used for missing/expired/invalid tokens.
    /// </summary>
    public const int CommunityModuleCap = 10;

    public static LicenseInfo Unlicensed(string? validationError = null) => new()
    {
        Tier = Tier.Community,
        IsValid = false,
        MaxModules = CommunityModuleCap,
        ValidationError = validationError
    };
}

public static class TierExtensions
{
    public static string ToClaimValue(this Tier tier) => tier switch
    {
        Tier.Community => "community",
        Tier.Lite => "lite",
        Tier.Enterprise => "enterprise",
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null)
    };

    public static Tier? FromClaimValue(string? raw) => raw switch
    {
        "community" => Tier.Community,
        "lite" => Tier.Lite,
        "enterprise" => Tier.Enterprise,
        _ => null
    };
}
