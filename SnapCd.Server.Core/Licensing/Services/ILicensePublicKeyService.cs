namespace SnapCd.Server.Core.Licensing.Services;

public interface ILicensePublicKeyService
{
    Task<string?> GetAsync(CancellationToken ct = default);
    Task<string?> RefreshFromRemoteAsync(CancellationToken ct = default);
}
