using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class OutputSet : AuditBase, IEntity, IModuleChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid ModuleId { get; set; }

    public long Timestamp { get; set; }

    [MaxLength(1024)] public string Checksum { get; set; } = null!;
    public List<Output> Outputs { get; set; } = new();

    [JsonIgnore] // So that JSON Serialization does not create a loop
    public Module Module { get; set; } = null!;

    public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleId;
    }
}