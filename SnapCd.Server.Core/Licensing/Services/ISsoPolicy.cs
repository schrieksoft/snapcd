namespace SnapCd.Server.Core.Licensing.Services;

public interface ISsoPolicy
{
    Task<bool> ShouldEnableSsoAsync(IServiceProvider serviceProvider);
}
