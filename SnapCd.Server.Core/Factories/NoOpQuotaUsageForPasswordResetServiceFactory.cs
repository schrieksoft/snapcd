using SnapCd.Server.Core.Services.QuotaUsage;

namespace SnapCd.Server.Core.Factories;

public class NoOpQuotaUsageForPasswordResetServiceFactory : IQuotaUsageForPasswordResetServiceFactory
{
    public IQuotaUsageForPasswordResetServiceScope Create() => new Scope();

    private sealed class Scope : IQuotaUsageForPasswordResetServiceScope
    {
        public IQuotaUsageForPasswordResetService Service { get; } = new NoOpQuotaUsageForPasswordResetService();
        public void Dispose() { }
    }
}
