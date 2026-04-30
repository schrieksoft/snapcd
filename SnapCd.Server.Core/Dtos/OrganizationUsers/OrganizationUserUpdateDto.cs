using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos.OrganizationUsers;

public class OrganizationUserUpdateDto : OrganizationUserCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
