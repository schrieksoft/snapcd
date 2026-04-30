using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Edition;

namespace SnapCd.Server.SelfHosted.Services;

public class SelfHostedOrganizationLimitPolicy : IOrganizationLimitPolicy
{
    public bool AllowsOrganizationCreation => false;

    public Task EnforceAsync(int currentActiveOrgCount)
    {
        if (currentActiveOrgCount >= 1)
        {
            throw new QuotaExceededException(
                "Organization",
                currentActiveOrgCount,
                1,
                "Self-hosted installations are limited to 1 organization.");
        }

        return Task.CompletedTask;
    }
}
