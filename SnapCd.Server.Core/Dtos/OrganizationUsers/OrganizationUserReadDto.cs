using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos.OrganizationUsers;

public class OrganizationUserReadDto : OrganizationUserCreateDto, IDto
{
    public Guid Id { get; set; }
}