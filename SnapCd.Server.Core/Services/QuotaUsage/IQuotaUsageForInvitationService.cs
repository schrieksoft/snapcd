namespace SnapCd.Server.Core.Services.QuotaUsage;

public interface IQuotaUsageForInvitationService
{
    Task CheckAndRecordInvitationAsync(Guid userId, Guid organizationId, string targetEmail);

    Task CleanupExpiredLogsAsync();
}
