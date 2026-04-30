namespace SnapCd.Server.Core.Services.Edition;

public interface IOrganizationCountValidator
{
    Task<bool> IsOverLimitAsync();
}
