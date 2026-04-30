namespace SnapCd.Contracts.Dto.RoleAssignments;

public class ServicePrincipalModuleRoleAssignmentCreateDto
{
    public Guid ServicePrincipalId { get; set; }

    public Guid ModuleId { get; set; }

    public ModuleRole RoleName { get; set; }
}
