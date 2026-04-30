namespace SnapCd.Server.Core.Services.Vaults;

/// <summary>
/// Result of a SetIfChanged operation, indicating the version and whether the value was changed.
/// </summary>
/// <param name="Version">The version identifier of the secret (new version if changed, current version if unchanged).</param>
/// <param name="WasChanged">True if the secret was created or updated, false if it already existed with the same value.</param>
public record SetIfChangedResult(string Version, bool WasChanged);

public interface IVault : IDisposable
{
    public Task<SetIfChangedResult> SetIfChanged(string secretName, string value);
    public Task<string> GetSecretAsync(string secretName);
    public Task<string> SetSecretAsync(string secretName, string value);

    public Task DeleteSecretAsync(string secretName);
}