namespace SnapCd.Server.Core.Services.QuotaUsage;

public class NoOpQuotaUsageForEmailConfirmationService : IQuotaUsageForEmailConfirmationService
{
    public Task<(bool Allowed, string Message)> CheckAndRecordAsync(Guid userId) =>
        Task.FromResult((true, "OK"));

    public Task<int> GetRemainingAttemptsAsync(Guid userId) => Task.FromResult(int.MaxValue);

    public Task CleanupExpiredLogsAsync() => Task.CompletedTask;
}
