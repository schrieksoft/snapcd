using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Interfaces;

public interface ISystemEntity
{
    public Guid Id { get; set; }

    // Audit fields
    public Guid CreatedBy { get; set; }
    public AuditPrincipalDiscriminator CreatedByPrincipalDiscriminator { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public Guid ModifiedBy { get; set; }
    public AuditPrincipalDiscriminator ModifiedByPrincipalDiscriminator { get; set; }
    public DateTime ModifiedDateTime { get; set; }
}