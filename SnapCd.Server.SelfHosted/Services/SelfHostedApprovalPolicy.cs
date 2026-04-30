using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;

namespace SnapCd.Server.SelfHosted.Services;

public class SelfHostedApprovalPolicy(LicenseService licenseService) : IApprovalPolicy
{
    public async Task<bool> ShouldAutoApproveAsync(Guid organizationId)
    {
        var licenseInfo = await licenseService.GetLicenseInfoAsync(organizationId);
        return licenseInfo is not { Edition: Edition.EnterpriseEdition, IsValid: true };
    }
}
