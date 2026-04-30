namespace SnapCd.Server.Core.Entities.Definition.GroupMembers;

public class ServicePrincipalGroupMember : GroupMember
{
    public Guid ServicePrincipalId { get; set; }

    public ServicePrincipal ServicePrincipal { get; set; } = null!;
}