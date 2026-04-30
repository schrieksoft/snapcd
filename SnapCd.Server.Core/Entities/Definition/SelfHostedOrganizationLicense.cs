using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;

namespace SnapCd.Server.Core.Entities.Definition;

public class SelfHostedOrganizationLicense : AuditBase
{
    public Guid OrganizationId { get; set; }
    public virtual Organization? Organization { get; set; }

    [MaxLength(4096)] public string? LicenseToken { get; set; }

    [MaxLength(255)] public string? SelfHostedLicenseKey { get; set; }

    public Guid? SelfHostedSubscriptionId { get; set; }

    // Cached RSA public key used to validate LicenseToken. Fetched from snapcd.io periodically.
    [MaxLength(4096)] public string? PublicKeyPem { get; set; }

    public DateTime? PublicKeyFetchedAtUtc { get; set; }
}
