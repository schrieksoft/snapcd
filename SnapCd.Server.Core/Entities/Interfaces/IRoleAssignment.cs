namespace SnapCd.Server.Core.Entities.Interfaces;

public interface IRoleAssignment
{
    public Guid PrincipalId { get; set; }

    public Guid OrganizationId { get; set; }
}
