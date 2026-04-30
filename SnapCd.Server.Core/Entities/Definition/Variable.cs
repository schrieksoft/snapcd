using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class Variable : AuditBase, IEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid VariableSetId { get; set; }
    [MaxLength(255)] public string Name { get; set; } = null!;
    [MaxLength(255)] public string? Type { get; set; }
    [MaxLength(2000)] public string? Description { get; set; }
    public bool Sensitive { get; set; } = false;
    public bool Nullable { get; set; } = false;
    public bool FromExtraFile { get; set; } = false;

    [JsonIgnore] // So that JSON Serialization does not create a loop
    public VariableSet VariableSet { get; set; } = null!;

    public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return VariableSetId;
    }
}