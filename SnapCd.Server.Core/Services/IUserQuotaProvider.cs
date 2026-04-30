namespace SnapCd.Server.Core.Services;

public interface IUserQuotaProvider
{
    /// <summary>
    /// Max number of organizations the given user is allowed to own.
    /// NoOp (self-hosted) returns unbounded; SaaS combines settings + per-user override.
    /// </summary>
    Task<int> GetOrganizationQuotaAsync(Guid userId, CancellationToken ct = default);
}

public class NoOpUserQuotaProvider : IUserQuotaProvider
{
    public Task<int> GetOrganizationQuotaAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult(int.MaxValue);
}
