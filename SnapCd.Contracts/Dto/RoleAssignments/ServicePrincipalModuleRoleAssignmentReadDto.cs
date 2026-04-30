using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class ServicePrincipalModuleRoleAssignmentReadDto : ServicePrincipalModuleRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}