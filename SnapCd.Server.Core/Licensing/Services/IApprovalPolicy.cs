namespace SnapCd.Server.Core.Licensing.Services;

public interface IApprovalPolicy
{
    Task<bool> ShouldAutoApproveAsync(Guid organizationId);
}
