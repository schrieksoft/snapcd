using System.ComponentModel.DataAnnotations.Schema;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Runner.Base;

public class RunnerRoleAssignment : AuditBase, IEntity, IOrganizationChild, IRunnerRoleAssignment
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid RunnerId { get; set; }

    public Organization Organization { get; set; } = null!;

    public Definition.Runner Runner { get; set; } = null!;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public Guid PrincipalId { get; set; }

    public RoleAssignmentPrincipalDiscriminator PrincipalDiscriminator { get; set; }
    public RunnerRole RoleName { get; set; }

    public virtual Guid ParentId()
    {
        return OrganizationId;
    }
}