namespace SnapCd.Contracts.Dto.RoleAssignments.Base;

public class ModuleRoleAssignmentCreateDto
{

    public Guid ModuleId { get; set; }

    public Guid PrincipalId { get; set; }

    public RoleAssignmentPrincipalDiscriminator PrincipalDiscriminator { get; set; }

    public ModuleRole RoleName { get; set; }
}
