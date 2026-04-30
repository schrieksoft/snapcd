using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleJobApprovals;

public class ModuleJobApprovalReadDto : ModuleJobApprovalCreateDto, IDto
{
    public Guid Id { get; set; }
}