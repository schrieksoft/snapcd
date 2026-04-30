namespace SnapCd.Server.Core.Entities.Interfaces;

public interface IGroupMember
{
    Guid PrincipalId { get; set; }
    Guid GroupId { get; set; }
    Guid OrganizationId { get; set; }
}