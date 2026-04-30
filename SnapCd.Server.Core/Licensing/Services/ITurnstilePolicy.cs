namespace SnapCd.Server.Core.Licensing.Services;

public interface ITurnstilePolicy
{
    Task<bool> ShouldEnableTurnstileAsync(IServiceProvider serviceProvider);
}
