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
using SnapCd.Runner.Hub;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Services;

public class AccessTokenCacheQuartzJob : IJob
{
    private readonly IMemoryCache _cache;
    private readonly RunnerSettings _runnerSettings;
    private readonly ServerSettings _serverSettings;
    private readonly ServicePrincipalTokenService _tokenService;
    private readonly RunnerHubConnection _hubConnection;
    private readonly ILogger<AccessTokenCacheQuartzJob> _logger;

    public AccessTokenCacheQuartzJob(
        IMemoryCache cache,
        IOptions<RunnerSettings> runnerSettings,
        IOptions<ServerSettings> serverSettings,
        ServicePrincipalTokenService tokenService,
        RunnerHubConnection hubConnection,
        ILogger<AccessTokenCacheQuartzJob> logger)
    {
        _cache = cache;
        _runnerSettings = runnerSettings.Value;
        _serverSettings = serverSettings.Value;
        _tokenService = tokenService;
        _hubConnection = hubConnection;
        _logger = logger;
    }

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refreshing token");

        try
        {
            var result = await GetTokenAsync(cancellationToken);

            if (result != null && result.ExpiresIn > 0)
            {
                var expirationTime = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
                var timeUntilExpiration = TimeSpan.FromSeconds(result.ExpiresIn);

                if (timeUntilExpiration > TimeSpan.Zero)
                {
                    _cache.Set(MemoryCacheConstants.AccessTokenCacheKey, result.AccessToken, timeUntilExpiration);
                    _cache.Set(MemoryCacheConstants.AccessTokenExpiryCacheKey, expirationTime);

                    _logger.LogInformation("Token refreshed. Expires at {ExpirationTime}", expirationTime);

                    // The server authenticates a hub connection once, against the token it was
                    // opened with, and closes it when that token expires. A cached token only
                    // reaches the server on a new connection.
                    await ReconnectHub(cancellationToken);

                    var triggerTime = expirationTime - RefreshLeadTime(timeUntilExpiration);

                    _logger.LogDebug("Next token refresh scheduled for {TriggerTime}", triggerTime);
                    await ScheduleNextExecution(context, triggerTime, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Token already expired or invalid expiration time");
                }
            }
            else
            {
                _logger.LogWarning("Token refresh failed. Scheduling retry in 30 seconds");
                await ScheduleNextExecution(context, DateTimeOffset.UtcNow.AddSeconds(30), cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed. Scheduling retry in 30 seconds");
            await ScheduleNextExecution(context, DateTimeOffset.UtcNow.AddSeconds(30), cancellationToken);
        }
    }

    /// <summary>
    /// How far ahead of expiry to fetch the next token. The server decides the lifetime and reports
    /// it as expires_in, so the lead scales with it: a fixed lead longer than the lifetime itself
    /// would schedule every refresh in the past and spin.
    /// </summary>
    /// <summary>
    /// Places the first refresh against the token obtained at startup. Quartz is configured before
    /// that token exists, so the trigger cannot be registered with the job.
    /// </summary>
    public static async Task ScheduleFirstRefresh(IServiceProvider services)
    {
        var cache = services.GetRequiredService<IMemoryCache>();
        var logger = services.GetRequiredService<ILogger<AccessTokenCacheQuartzJob>>();
        unner var scheduler = await services.GetRequiredService<ISchedulerFactory>().GetScheduler();

        var expiry = cache.Get<DateTime?>(MemoryCacheConstants.AccessTokenExpiryCacheKey);

        var triggerTime = expiry is { } e
            ? e - RefreshLeadTime(e - DateTime.UtcNow)
            : DateTime.UtcNow;

        if (triggerTime <= DateTime.UtcNow) triggerTime = DateTime.UtcNow.AddSeconds(1);

        logger.LogInformation("First token refresh scheduled for {TriggerTime}", triggerTime);

        await scheduler.ScheduleJob(
            TriggerBuilder.Create()
                .ForJob(nameof(AccessTokenCacheQuartzJob))
                .WithIdentity($"{nameof(AccessTokenCacheQuartzJob)}First")
                .StartAt(triggerTime)
                .Build());
    }

    public static TimeSpan RefreshLeadTime(TimeSpan lifetime)
    {
        var lead = lifetime * 0.2;

        return lead > MaxRefreshLead ? MaxRefreshLead : lead;
    }

    /// <summary>Caps the lead so a long-lived token is not fetched needlessly early.</summary>
    private static readonly TimeSpan MaxRefreshLead = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Opens a connection carrying the refreshed token. A failure here is not fatal: the old
    /// connection stays up until its token expires, and the close handler reconnects after it.
    /// </summary>
    private async Task ReconnectHub(CancellationToken cancellationToken)
    {
        try
        {
            await _hubConnection.DisconnectAndReconnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not reconnect the hub with the refreshed token");
        }
    }

    private async Task<TokenResponse?> GetTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = _runnerSettings.Credentials.Mode == CredentialMode.Federated
                ? await _tokenService.GetAccessTokenWithAssertionAsync(
                    _serverSettings.Url,
                    _runnerSettings.OrganizationId,
                    _runnerSettings.Credentials.ClientId,
                    _runnerSettings.Credentials.AssertionTokenPath,
                    cancellationToken)
                : await _tokenService.GetAccessTokenAsync(
                    _serverSettings.Url,
                    _runnerSettings.OrganizationId,
                    _runnerSettings.Credentials.ClientId,
                    _runnerSettings.Credentials.ClientSecret,
                    cancellationToken);

            return result;
        }
        catch
        {
            return null;
        }
    }

    private async Task ScheduleNextExecution(IJobExecutionContext context, DateTimeOffset nextExecutionTime, CancellationToken cancellationToken)
    {
        var trigger = TriggerBuilder.Create()
            .StartAt(nextExecutionTime)
            .ForJob(context.JobDetail)
            .Build();

        await context.Scheduler.ScheduleJob(trigger, new ScheduleJobOptions(), cancellationToken);
    }
}