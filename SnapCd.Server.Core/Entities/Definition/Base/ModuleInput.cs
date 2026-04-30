using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.Base;

public class ModuleInput : AuditBase, IEntity, IModuleChild, IModuleInput
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid ModuleId { get; set; }
    [MaxLength(255)] public string Name { get; set; } = null!;

    public virtual InputKind InputKind { get; init; }

    [JsonIgnore] // So that JSON Serialization does not create a loop
    public virtual Organization Organization { get; set; } = null!;

    [Required]
    [JsonIgnore] // So that JSON Serialization does not create a loop
    public Module Module { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleId;
    }
}