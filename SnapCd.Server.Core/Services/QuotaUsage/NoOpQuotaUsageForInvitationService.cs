namespace SnapCd.Server.Core.Services.QuotaUsage;

public class NoOpQuotaUsageForInvitationService : IQuotaUsageForInvitationService
{
    public Task CheckAndRecordInvitationAsync(Guid userId, Guid organizationId, string targetEmail) =>
        Task.CompletedTask;

    public Task CleanupExpiredLogsAsync() => Task.CompletedTask;
}
