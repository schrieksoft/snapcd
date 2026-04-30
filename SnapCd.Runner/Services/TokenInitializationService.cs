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

    public TokenInitializationService(
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

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("Starting token initialization. The app will not start up until this has succeeded.");

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
                        Console.WriteLine($"Initial token obtained. Expires at: {expirationTime}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Initial token acquisition failed: {ex.Message}");
            }

            if (!tokenObtained) await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        }
    }
}