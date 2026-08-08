// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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
    private readonly ILogger<AccessTokenCacheQuartzJob> _logger;

    public AccessTokenCacheQuartzJob(
        IMemoryCache cache,
        IOptions<RunnerSettings> runnerSettings,
        IOptions<ServerSettings> serverSettings,
        ServicePrincipalTokenService tokenService,
        ILogger<AccessTokenCacheQuartzJob> logger)
    {
        _cache = cache;
        _runnerSettings = runnerSettings.Value;
        _serverSettings = serverSettings.Value;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogDebug("Refreshing token");

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

                    _logger.LogInformation("Token refreshed. Expires at {ExpirationTime}", expirationTime);

                    // Schedule the next execution 5 minutes before the expiration
                    var triggerTime = expirationTime.AddMinutes(-5);

                    if (triggerTime > DateTime.UtcNow)
                    {
                        _logger.LogDebug("Next token refresh scheduled for {TriggerTime}", triggerTime);
                        await ScheduleNextExecution(context, triggerTime);
                    }
                    else
                    {
                        _logger.LogDebug("Trigger time is in the past. Immediate re-run scheduled.");
                        await context.Scheduler.TriggerJob(context.JobDetail.Key);
                    }
                }
                else
                {
                    _logger.LogWarning("Token already expired or invalid expiration time");
                }
            }
            else
            {
                _logger.LogWarning("Token refresh failed. Scheduling retry in 30 seconds");
                await ScheduleNextExecution(context, DateTimeOffset.UtcNow.AddSeconds(30));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed. Scheduling retry in 30 seconds");
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