namespace SnapCd.Server.Core.Licensing.Services;

public static class SsoGatingService
{
    public static async Task<bool> ShouldEnableSsoAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var ssoPolicy = scope.ServiceProvider.GetRequiredService<ISsoPolicy>();
        return await ssoPolicy.ShouldEnableSsoAsync(serviceProvider);
    }
}
