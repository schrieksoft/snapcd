using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Secrets;

/// <summary>
/// DTO for Secret responses (GET operations).
/// </summary>
public class SecretDto : SecretCreateDto, IDto
{
    public Guid Id { get; set; }
}
