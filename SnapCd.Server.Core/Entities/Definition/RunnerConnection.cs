using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// Represents an active runner connection to a specific server instance.
/// Used for connection management, duplicate detection, and server crash recovery.
/// </summary>
public class RunnerConnection : AuditBase, IEntity, IOrganizationChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RunnerId { get; set; }

    [MaxLength(255)] public string InstanceName { get; set; } = null!;

    [MaxLength(255)] public string SignalRConnectionId { get; set; } = null!;

    public Guid ServerInstanceId { get; set; }

    // Navigation properties
    public virtual Organization Organization { get; set; } = null!;
    public virtual Runner Runner { get; set; } = null!;

    public Guid ParentId()
    {
        return OrganizationId;
    }
}
