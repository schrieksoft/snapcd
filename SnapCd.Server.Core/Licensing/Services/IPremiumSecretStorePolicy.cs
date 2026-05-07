namespace SnapCd.Server.Core.Licensing.Services;

/// <summary>
/// Answers whether a new vault may be created right now. Returns true when the configured
/// secret-store provider is SqlServer (always allowed) OR the active licence covers the
/// PremiumSecretStore feature. Self-Hosted checks the licence; SaaS always returns true.
/// </summary>
public interface IPremiumSecretStorePolicy
{
    Task<bool> IsAllowedAsync(CancellationToken ct = default);
}
