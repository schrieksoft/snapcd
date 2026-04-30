using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class ServicePrincipalModuleRoleAssignmentUpdateDto : ServicePrincipalModuleRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
