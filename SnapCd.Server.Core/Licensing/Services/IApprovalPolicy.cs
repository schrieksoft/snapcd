namespace SnapCd.Server.Core.Licensing.Services;

public interface IApprovalPolicy
{
    /// <summary>
    /// Returns true when the organization's tier includes the ApprovalWorkflows feature
    /// (i.e. approvals can be enforced before apply/destroy). When false, the org cannot
    /// use approval thresholds — jobs auto-approve when no threshold is configured, or
    /// fail with NotApproved when one is.
    /// </summary>
    Task<bool> SupportsApprovalWorkflowsAsync(Guid organizationId);
}
