using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Events.Repository.System.Base;

/// <summary>
/// System Update Event Transfer Object for system-scoped entities (non-organization-scoped).
/// Contains both previous and current state with audit metadata without organization context.
/// </summary>
public class SystemUpdatedEvent<TDto>
{
    /// <summary>
    /// The previous DTO payload before the update
    /// </summary>
    public TDto PreviousData { get; set; } = default!;

    /// <summary>
    /// The current DTO payload after the update
    /// </summary>
    public TDto Data { get; set; } = default!;

    /// <summary>
    /// ID of the principal that created this entity
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Type of principal that created this entity (User or ServicePrincipal)
    /// </summary>
    public AuditPrincipalDiscriminator CreatedByPrincipalDiscriminator { get; set; }

    /// <summary>
    /// UTC timestamp when this entity was created
    /// </summary>
    public DateTime CreatedDateTime { get; set; }

    /// <summary>
    /// ID of the principal that last modified this entity (current state)
    /// </summary>
    public Guid ModifiedBy { get; set; }

    /// <summary>
    /// Type of principal that last modified this entity (current state)
    /// </summary>
    public AuditPrincipalDiscriminator ModifiedByPrincipalDiscriminator { get; set; }

    /// <summary>
    /// UTC timestamp when this entity was last modified (current state)
    /// </summary>
    public DateTime ModifiedDateTime { get; set; }

    /// <summary>
    /// ID of the principal that last modified this entity (previous state)
    /// </summary>
    public Guid PreviousModifiedBy { get; set; }

    /// <summary>
    /// Type of principal that last modified this entity (previous state)
    /// </summary>
    public AuditPrincipalDiscriminator PreviousModifiedByPrincipalDiscriminator { get; set; }

    /// <summary>
    /// UTC timestamp when this entity was last modified (previous state)
    /// </summary>
    public DateTime PreviousModifiedDateTime { get; set; }
}
