using SnapCd.Server.Core.Services.QuotaUsage;

namespace SnapCd.Server.Core.Factories;

public interface IQuotaUsageForPasswordResetServiceFactory
{
    IQuotaUsageForPasswordResetServiceScope Create();
}

public interface IQuotaUsageForPasswordResetServiceScope : IDisposable
{
    IQuotaUsageForPasswordResetService Service { get; }
}
