using System.ComponentModel.DataAnnotations.Schema;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;

public class StackRoleAssignment : AuditBase, IEntity, IOrganizationChild, IStackRoleAssignment, IStackChild
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid StackId { get; set; }

    public Organization Organization { get; set; } = null!;

    public Stack Stack { get; set; } = null!;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public Guid PrincipalId { get; set; }

    public RoleAssignmentPrincipalDiscriminator PrincipalDiscriminator { get; set; }
    public StackRole RoleName { get; set; }

    public virtual Guid ParentId()
    {
        return StackId;
    }
}