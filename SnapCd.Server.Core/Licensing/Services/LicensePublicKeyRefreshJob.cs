namespace SnapCd.Server.Core.Licensing.Services;

public class LicensePublicKeyRefreshJob(ILicensePublicKeyService service)
{
    public Task ExecuteJob() => service.RefreshFromRemoteAsync();
}
