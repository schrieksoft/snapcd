using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromSecretReadDto : ModuleInputFromSecretCreateDto, IDto
{
    public Guid Id { get; set; }
}