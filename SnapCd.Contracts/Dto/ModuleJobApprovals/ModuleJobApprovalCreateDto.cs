namespace SnapCd.Contracts.Dto.ModuleJobApprovals;

public class ModuleJobApprovalCreateDto
{
    public Guid ModuleJobId { get; set; }
    public Guid PrincipalId { get; set; }
    public PrincipalDiscriminator PrincipalDiscriminator { get; set; }
    public DateTime DecisionDateTime { get; set; }
    public bool Declined { get; set; }
}
