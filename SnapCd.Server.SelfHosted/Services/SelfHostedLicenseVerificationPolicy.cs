using SnapCd.Server.Core.Licensing.Services;

namespace SnapCd.Server.SelfHosted.Services;

public class SelfHostedLicenseVerificationPolicy : ILicenseVerificationPolicy
{
    public bool ShouldSkipVerification => false;
}
