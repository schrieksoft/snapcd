using System.ComponentModel.DataAnnotations;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class ModuleTerraformArrayFlag : AuditBase, IEntity, IModuleChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public TerraformCommandTask Task { get; set; }
    public TerraformArrayFlag Flag { get; set; }

    [MaxLength(1000)] public string Value { get; set; } = null!;

    public Guid ModuleId { get; set; }
    public virtual Module Module { get; set; } = null!;

    public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleId;
    }
}
