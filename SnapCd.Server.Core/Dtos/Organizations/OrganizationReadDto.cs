using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos.Organizations;

/// <summary>
/// DTO for Organization responses (GET operations).
/// </summary>
public class OrganizationReadDto : OrganizationCreateDto, IDto
{
    public Guid Id { get; set; }

    public DateTime CreatedDateTime { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
