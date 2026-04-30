using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleJobApprovals;

public class ModuleJobApprovalUpdateDto : ModuleJobApprovalCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
