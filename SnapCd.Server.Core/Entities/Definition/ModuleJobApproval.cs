using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

public class ModuleJobApproval : AuditBase, IEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ModuleJobId { get; set; }

    public Guid PrincipalId { get; set; }

    public PrincipalDiscriminator PrincipalDiscriminator { get; set; }

    public DateTime DecisionDateTime { get; set; }

    public bool Declined { get; set; }

    [JsonIgnore] public ModuleJob ModuleJob { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleJobId;
    }
}