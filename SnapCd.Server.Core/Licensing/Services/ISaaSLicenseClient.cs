namespace SnapCd.Server.Core.Licensing.Services;

public interface ISaaSLicenseClient
{
    Task<SaaSLicenseResponse?> IssueAsync(string licenseKey, CancellationToken ct = default);
    Task<SaaSLicenseResponse?> RefreshAsync(string licenseKey, string? currentToken, CancellationToken ct = default);
}

public record SaaSLicenseResponse(string Token, DateTime ExpiresAtUtc, DateTime LicensePeriodEndUtc);
