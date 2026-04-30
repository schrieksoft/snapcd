using SnapCd.Server.Core.Services.QuotaUsage;

namespace SnapCd.Server.Core.Factories;

public class NoOpQuotaUsageForInvitationServiceFactory : IQuotaUsageForInvitationServiceFactory
{
    public IQuotaUsageForInvitationServiceScope Create() => new Scope();

    private sealed class Scope : IQuotaUsageForInvitationServiceScope
    {
        public IQuotaUsageForInvitationService Service { get; } = new NoOpQuotaUsageForInvitationService();
        public void Dispose() { }
    }
}
