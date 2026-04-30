using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition.Base;

public class AuditBase
{
    // Audit fields
    public Guid CreatedBy { get; set; }

    public AuditPrincipalDiscriminator CreatedByPrincipalDiscriminator { get; set; }

    public DateTime CreatedDateTime { get; set; }
    public Guid ModifiedBy { get; set; }

    public AuditPrincipalDiscriminator ModifiedByPrincipalDiscriminator { get; set; }

    public DateTime ModifiedDateTime { get; set; }
}