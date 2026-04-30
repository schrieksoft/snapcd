using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

[Obsolete("Use NamespaceTerraformFlag entities instead.")]
public class NamespaceBackendConfig : AuditBase, IEntity, INamespaceChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    [MaxLength(255)] public string Name { get; set; } = null!;

    [MaxLength(1000)] public string Value { get; set; } = null!;

    public Guid NamespaceId { get; set; }
    public virtual Namespace Namespace { get; set; } = null!;

    public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return NamespaceId;
    }
}