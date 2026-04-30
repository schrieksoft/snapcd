using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos.Organizations;

/// <summary>
/// DTO for updating an existing Organization (PUT operations).
/// </summary>
public class OrganizationUpdateDto : OrganizationCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
