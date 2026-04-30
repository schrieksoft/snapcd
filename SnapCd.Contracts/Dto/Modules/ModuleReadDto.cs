using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Modules;

/// <summary>
/// DTO for Module responses (GET operations).
/// </summary>
public class ModuleReadDto : ModuleCreateDto, IDto
{
    public Guid Id { get; set; }
}
