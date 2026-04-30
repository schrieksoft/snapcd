using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.DependsOnModules;

/// <summary>
/// DTO for DependsOnModule responses (GET operations).
/// </summary>
public class DependsOnModuleReadDto : DependsOnModuleCreateDto, IDto
{
    public Guid Id { get; set; }
}
