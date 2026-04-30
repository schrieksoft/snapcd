using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Secrets;

public class MappedSecretDto : MappedSecretCreateDto, IDto
{
    public Guid Id { get; set; }
}