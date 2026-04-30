using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Secrets;

public class MappedSecretUpdateDto : MappedSecretCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
