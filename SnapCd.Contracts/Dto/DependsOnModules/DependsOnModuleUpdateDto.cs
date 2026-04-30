using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.DependsOnModules;

/// <summary>
/// DTO for updating an existing DependsOnModule (PUT operations).
/// </summary>
public class DependsOnModuleUpdateDto : DependsOnModuleCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
