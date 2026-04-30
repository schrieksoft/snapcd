using SnapCd.Server.Core.Services.QuotaUsage;

namespace SnapCd.Server.Core.Factories;

public interface IQuotaUsageForEmailConfirmationServiceFactory
{
    IQuotaUsageForEmailConfirmationServiceScope Create();
}

public interface IQuotaUsageForEmailConfirmationServiceScope : IDisposable
{
    IQuotaUsageForEmailConfirmationService Service { get; }
}
