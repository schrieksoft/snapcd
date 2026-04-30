using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition.Secrets;

public class Secret : AuditBase, IEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    [MaxLength(255)] public string Name { get; set; } = null!;

    public virtual SecretScope ScopeKind { get; init; }

    public virtual Organization Organization { get; set; } = null!;

    // Injected factories
    public virtual Guid ParentId()
    {
        return OrganizationId;
    }
}