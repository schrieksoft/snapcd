// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SnapCd.Runner.Constants;
using SnapCd.Runner.Settings;

namespace SnapCd.Runner.Services;

// 1. Create a token initialization service
public class TokenInitializationService
{
    private readonly IMemoryCache _cache;
    private readonly RunnerSettings _runnerSettings;
    private readonly ServerSettings _serverSettings;
    private readonly ServicePrincipalTokenService _tokenService;
    private readonly ILogger<TokenInitializationService> _logger;

    public TokenInitializationService(
        IMemoryCache cache,
        IOptions<RunnerSettings> runnerSettings,
        IOptions<ServerSettings> serverSettings,
        ServicePrincipalTokenService tokenService,
        ILogger<TokenInitializationService> logger)
    {
        _cache = cache;
        _runnerSettings = runnerSettings.Value;
        _serverSettings = serverSettings.Value;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting token initialization. The app will not start up until this has succeeded.");

        var tokenObtained = false;
        while (!tokenObtained && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _tokenService.GetAccessTokenAsync(
                    _serverSettings.Url,
                    _runnerSettings.OrganizationId,
                    _runnerSettings.Credentials.ClientId,
                    _runnerSettings.Credentials.ClientSecret,
                    cancellationToken);

                if (result.ExpiresIn > 0)
                {
                    var expirationTime = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
                    var timeUntilExpiration = TimeSpan.FromSeconds(result.ExpiresIn);

                    if (timeUntilExpiration > TimeSpan.Zero)
                    {
                        _cache.Set(MemoryCacheConstants.AccessTokenCacheKey, result.AccessToken, timeUntilExpiration);
                        _cache.Set(MemoryCacheConstants.AccessTokenExpiryCacheKey, expirationTime);
                        tokenObtained = true;
                        _logger.LogInformation("Initial token obtained. Expires at {ExpirationTime}", expirationTime);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Initial token acquisition failed");
            }

            if (!tokenObtained) await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        }
    }
}