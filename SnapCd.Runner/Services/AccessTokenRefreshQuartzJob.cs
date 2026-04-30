using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Quartz;
using SnapCd.Runner.Constants;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Services;

public class AccessTokenCacheQuartzJob : IJob
{
    private readonly IMemoryCache _cache;
    private readonly RunnerSettings _runnerSettings;
    private readonly ServerSettings _serverSettings;
    private readonly ServicePrincipalTokenService _tokenService;

    public AccessTokenCacheQuartzJob(
        IMemoryCache cache,
        IOptions<RunnerSettings> runnerSettings,
        IOptions<ServerSettings> serverSettings,
        ServicePrincipalTokenService tokenService)
    {
        _cache = cache;
        _runnerSettings = runnerSettings.Value;
        _serverSettings = serverSettings.Value;
        _tokenService = tokenService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        Console.WriteLine("Refreshing token...");

        try
        {
            var result = await GetTokenAsync();

            if (result != null && result.ExpiresIn > 0)
            {
                var expirationTime = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
                var timeUntilExpiration = TimeSpan.FromSeconds(result.ExpiresIn);

                if (timeUntilExpiration > TimeSpan.Zero)
                {
                    _cache.Set(MemoryCacheConstants.AccessTokenCacheKey, result.AccessToken, timeUntilExpiration);
                    _cache.Set(MemoryCacheConstants.AccessTokenExpiryCacheKey, expirationTime);

                    Console.WriteLine($"Token refreshed. Expires at: {expirationTime}");

                    // Schedule the next execution 5 minutes before the expiration
                    var triggerTime = expirationTime.AddMinutes(-5);

                    if (triggerTime > DateTime.UtcNow)
                    {
                        Console.WriteLine($"Next token refresh scheduled for: {triggerTime}");
                        await ScheduleNextExecution(context, triggerTime);
                    }
                    else
                    {
                        Console.WriteLine("Trigger time is in the past. Immediate re-run scheduled.");
                        await context.Scheduler.TriggerJob(context.JobDetail.Key);
                    }
                }
                else
                {
                    Console.WriteLine("Token already expired or invalid expiration time.");
                }
            }
            else
            {
                Console.WriteLine("Token refresh failed. Scheduling retry in 30 seconds.");
                await ScheduleNextExecution(context, DateTimeOffset.UtcNow.AddSeconds(30));
            }
        }
        catch
        {
            Console.WriteLine("Token refresh failed. Scheduling retry in 30 seconds.");
            await ScheduleNextExecution(context, DateTimeOffset.UtcNow.AddSeconds(30));
        }
    }

    private async Task<TokenResponse?> GetTokenAsync()
    {
        try
        {
            var result = await _tokenService.GetAccessTokenAsync(
                _serverSettings.Url,
                _runnerSettings.OrganizationId,
                _runnerSettings.Credentials.ClientId,
                _runnerSettings.Credentials.ClientSecret);

            return result;
        }
        catch
        {
            return null;
        }
    }

    private async Task ScheduleNextExecution(IJobExecutionContext context, DateTimeOffset nextExecutionTime)
    {
        var trigger = TriggerBuilder.Create()
            .StartAt(nextExecutionTime)
            .ForJob(context.JobDetail)
            .Build();

        await context.Scheduler.ScheduleJob(trigger);
    }
}