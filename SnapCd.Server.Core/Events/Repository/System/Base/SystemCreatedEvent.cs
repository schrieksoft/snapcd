using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Events.Repository.System.Base;

/// <summary>
/// System Create Event Transfer Object for system-scoped entities (non-organization-scoped).
/// Contains a DTO with audit metadata for create operations without organization context.
/// </summary>
public class SystemCreatedEvent<TDto>
{
    /// <summary>
    /// The DTO payload containing the business data
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
    /// ID of the principal that last modified this entity
    /// </summary>
    public Guid ModifiedBy { get; set; }

    /// <summary>
    /// Type of principal that last modified this entity (User or ServicePrincipal)
    /// </summary>
    public AuditPrincipalDiscriminator ModifiedByPrincipalDiscriminator { get; set; }

    /// <summary>
    /// UTC timestamp when this entity was last modified
    /// </summary>
    public DateTime ModifiedDateTime { get; set; }
}
