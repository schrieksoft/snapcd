using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition;

public class PreviewFeatureAcceptance : AuditBase, IEntity, IOrganizationChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public PreviewFeature PreviewFeature { get; set; }
    public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return OrganizationId;
    }
}
