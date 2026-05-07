using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;

namespace SnapCd.Server.Host.Services;

public class SelfHostedApprovalPolicy(LicenseService licenseService) : IApprovalPolicy
{
    public async Task<bool> SupportsApprovalWorkflowsAsync(Guid organizationId)
    {
        var licenseInfo = await licenseService.GetLicenseInfoAsync(organizationId);
        return licenseInfo.Includes(Feature.ApprovalWorkflows);
    }
}
