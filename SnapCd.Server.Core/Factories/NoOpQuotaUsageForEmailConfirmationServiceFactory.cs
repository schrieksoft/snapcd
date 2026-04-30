using SnapCd.Server.Core.Services.QuotaUsage;

namespace SnapCd.Server.Core.Factories;

public class NoOpQuotaUsageForEmailConfirmationServiceFactory : IQuotaUsageForEmailConfirmationServiceFactory
{
    public IQuotaUsageForEmailConfirmationServiceScope Create() => new Scope();

    private sealed class Scope : IQuotaUsageForEmailConfirmationServiceScope
    {
        public IQuotaUsageForEmailConfirmationService Service { get; } = new NoOpQuotaUsageForEmailConfirmationService();
        public void Dispose() { }
    }
}
