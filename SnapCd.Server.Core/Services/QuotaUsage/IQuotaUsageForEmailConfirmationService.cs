namespace SnapCd.Server.Core.Services.QuotaUsage;

public interface IQuotaUsageForEmailConfirmationService
{
    Task<(bool Allowed, string Message)> CheckAndRecordAsync(Guid userId);

    Task<int> GetRemainingAttemptsAsync(Guid userId);

    Task CleanupExpiredLogsAsync();
}
