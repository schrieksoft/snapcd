using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.Base;

public class NamespaceInput : AuditBase, IEntity, INamespaceChild, INamespaceInput
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid NamespaceId { get; set; }
    [MaxLength(255)] public string Name { get; set; } = null!;

    public virtual InputKind InputKind { get; init; }

    public NamespaceInputUsageMode UsageMode { get; set; }


    [JsonIgnore] // So that JSON Serialization does not create a loop
    public virtual Organization Organization { get; set; } = null!;

    [JsonIgnore] // So that JSON Serialization does not create a loop
    public Namespace Namespace { get; set; } = null!;

    public Guid ParentId()
    {
        return NamespaceId;
    }
}