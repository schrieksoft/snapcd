using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.RunnerAssignments;

public class RunnerStackAssignment : AuditBase, IEntity, IOrganizationChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid RunnerId { get; set; }

    public Guid StackId { get; set; }

    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;
    [JsonIgnore] public Runner Runner { get; set; } = null!;

    [JsonIgnore] public Stack Stack { get; set; } = null!;


    public Guid ParentId()
    {
        return RunnerId;
    }
}