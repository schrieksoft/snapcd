using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class Output : AuditBase, IEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid OutputSetId { get; set; }
    [MaxLength(255)] public string Name { get; set; } = null!;
    [MaxLength(255)] public string Type { get; set; } = null!;
    public bool FromExtraFile { get; set; } = false;

    [JsonIgnore] // So that JSON Serialization does not create a loop
    public OutputSet OutputSet { get; set; } = null!;

    public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return OutputSetId;
    }
}