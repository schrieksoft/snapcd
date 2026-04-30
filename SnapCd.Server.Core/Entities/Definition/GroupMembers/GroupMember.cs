using System.ComponentModel.DataAnnotations.Schema;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.GroupMembers;

public class GroupMember : AuditBase, IEntity, IOrganizationChild, IGroupMember
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid GroupId { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public Guid PrincipalId { get; set; }

    public GroupMemberDiscriminator GroupMemberDiscriminator { get; set; }
    public virtual Group Group { get; set; } = null!;

    public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return GroupId;
    }
}