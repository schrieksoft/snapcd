using System.ComponentModel.DataAnnotations;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class NamespacePulumiArrayFlag : AuditBase, IEntity, INamespaceChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public PulumiCommandTask Task { get; set; }
    public PulumiArrayFlag Flag { get; set; }

    [MaxLength(1000)] public string Value { get; set; } = null!;

    public Guid NamespaceId { get; set; }
    public virtual Namespace Namespace { get; set; } = null!;

    public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return NamespaceId;
    }
}
