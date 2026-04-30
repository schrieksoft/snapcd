namespace SnapCd.Server.Core.Licensing.Services;

public interface ILicenseVerificationPolicy
{
    bool ShouldSkipVerification { get; }
}
