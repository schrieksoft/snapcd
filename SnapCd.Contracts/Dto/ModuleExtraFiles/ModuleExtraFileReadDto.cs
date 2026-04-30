using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleExtraFiles;

/// <summary>
/// DTO for ModuleExtraFile responses (GET operations).
/// </summary>
public class ModuleExtraFileReadDto : ModuleExtraFileCreateDto, IDto
{
    public Guid Id { get; set; }
}
