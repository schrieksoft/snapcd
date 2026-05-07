namespace SnapCd.Server.Core.Licensing.Services;

/// <summary>
/// Answers whether a new bus-publishing operation may be created right now. Returns true when
/// the configured bus is SqlServer (always allowed) OR the active licence covers the
/// PremiumMessageBroker feature. Self-Hosted checks the licence; SaaS always returns true.
/// </summary>
public interface IPremiumMessageBrokerPolicy
{
    Task<bool> IsAllowedAsync(CancellationToken ct = default);
}
