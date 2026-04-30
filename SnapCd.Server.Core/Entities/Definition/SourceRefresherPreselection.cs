using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class SourceRefresherPreselection : AuditBase, IEntity, IOrganizationChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid RunnerId { get; set; }

    [MaxLength(255)] public string? RunnerInstanceName { get; set; }

    [MaxLength(800)] public string SourceUrl { get; set; } = null!;

    public Runner Runner { get; set; } = null!;
    public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return RunnerId;
    }
}