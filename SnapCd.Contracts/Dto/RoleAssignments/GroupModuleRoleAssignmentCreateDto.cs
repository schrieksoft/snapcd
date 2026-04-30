namespace SnapCd.Contracts.Dto.RoleAssignments;

public class GroupModuleRoleAssignmentCreateDto
{
    public Guid GroupId { get; set; }

    public Guid ModuleId { get; set; }

    public ModuleRole RoleName { get; set; }
}
