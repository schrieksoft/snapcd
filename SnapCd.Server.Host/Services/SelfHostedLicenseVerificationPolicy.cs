using SnapCd.Server.Core.Licensing.Services;

namespace SnapCd.Server.Host.Services;

public class SelfHostedLicenseVerificationPolicy : ILicenseVerificationPolicy
{
    public bool ShouldSkipVerification => false;
}
