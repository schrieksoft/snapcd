using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class ServicePrincipalNamespaceRoleAssignmentReadDto : ServicePrincipalNamespaceRoleAssignmentCreateDto, IDto
{
    public Guid Id { get; set; }
}