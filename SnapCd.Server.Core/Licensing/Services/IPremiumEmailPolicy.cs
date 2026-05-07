namespace SnapCd.Server.Core.Licensing.Services;

/// <summary>
/// Answers whether the running deployment may use a premium (non-NoOp) email sender.
/// Self-Hosted checks the licence tier; SaaS always returns true.
/// </summary>
public interface IPremiumEmailPolicy
{
    Task<bool> IsAllowedAsync(CancellationToken ct = default);
}
