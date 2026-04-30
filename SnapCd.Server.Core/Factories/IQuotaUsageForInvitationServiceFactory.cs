using SnapCd.Server.Core.Services.QuotaUsage;

namespace SnapCd.Server.Core.Factories;

public interface IQuotaUsageForInvitationServiceFactory
{
    IQuotaUsageForInvitationServiceScope Create();
}

public interface IQuotaUsageForInvitationServiceScope : IDisposable
{
    IQuotaUsageForInvitationService Service { get; }
}
