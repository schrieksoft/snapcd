namespace SnapCd.Server.Core.Services.Edition;

public interface IOrganizationLimitPolicy
{
    Task EnforceAsync(int currentActiveOrgCount);
    bool AllowsOrganizationCreation { get; }
}
