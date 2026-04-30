namespace SnapCd.Server.Core.Views;

/// <summary>
/// View model containing only the vault URL information needed to retrieve a secret's value.
/// Used to avoid loading the entire Secret and Organization entities when fetching remote secrets.
/// </summary>
public class SecretVaultInfoView
{
    /// <summary>
    /// The Key Vault URL configured for the organization's input secrets.
    /// Null if using the default vault URL.
    /// </summary>
    public string? InputKeyVaultUrl { get; init; }
}