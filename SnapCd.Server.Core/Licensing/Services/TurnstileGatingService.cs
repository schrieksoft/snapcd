namespace SnapCd.Server.Core.Licensing.Services;

public static class TurnstileGatingService
{
    public static async Task<bool> ShouldEnableTurnstileAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var policy = scope.ServiceProvider.GetRequiredService<ITurnstilePolicy>();
        return await policy.ShouldEnableTurnstileAsync(serviceProvider);
    }
}
