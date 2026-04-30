namespace SnapCd.Server.Core.Licensing.Models;

public enum Edition
{
    CommunityEdition,
    EnterpriseEdition
}

public class LicenseInfo
{
    public Edition Edition { get; set; }
    public bool IsValid { get; set; }
    public int? MaxModules { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? LicensePeriodEnd { get; set; }
    public Guid? SubscriptionId { get; set; }
    public string? ValidationError { get; set; }
}
