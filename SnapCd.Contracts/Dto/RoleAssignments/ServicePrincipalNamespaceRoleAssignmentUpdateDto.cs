using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.RoleAssignments;

public class ServicePrincipalNamespaceRoleAssignmentUpdateDto : ServicePrincipalNamespaceRoleAssignmentCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
