using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class ModuleExtraFile : AuditBase, IEntity, IModuleChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid ModuleId { get; set; }

    public Module Module { get; set; } = null!;

    [MaxLength(500)] public string FileName { get; set; } = null!;

    [MaxLength(4000)] public string Contents { get; set; } = null!;

    public bool Overwrite { get; set; }

    public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleId;
    }
}