using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// Represents the association between a runner connection and a module job,
/// tracking which specific task is being executed on that connection.
/// </summary>
public class RunnerConnectionJob : AuditBase, IEntity, IOrganizationChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RunnerConnectionId { get; set; }
    public Guid ModuleJobId { get; set; }

    [MaxLength(255)] public string TaskName { get; set; } = null!;

    // Navigation properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual RunnerConnection RunnerConnection { get; set; } = null!;
    public virtual ModuleJob ModuleJob { get; set; } = null!;

    public Guid ParentId()
    {
        return OrganizationId;
    }
}
