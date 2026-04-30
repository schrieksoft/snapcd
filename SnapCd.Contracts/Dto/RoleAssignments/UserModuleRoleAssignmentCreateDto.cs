namespace SnapCd.Contracts.Dto.RoleAssignments;

public class UserModuleRoleAssignmentCreateDto
{
    public Guid UserId { get; set; }

    public Guid ModuleId { get; set; }

    public ModuleRole RoleName { get; set; }
}
