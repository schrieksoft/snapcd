using System.Runtime.CompilerServices;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Services.Vaults;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Factories.Vaults;

public class AzureVaultFactory : IVaultFactory
{
    private readonly SecretStoreSettings _secretStoreSettings;
    private readonly ILoggerFactory _loggerFactory;

    public AzureVaultFactory(IOptions<SecretStoreSettings> secretStoreSettings, ILoggerFactory loggerFactory)
    {
        _secretStoreSettings = secretStoreSettings.Value;
        _loggerFactory = loggerFactory;
    }

    public IVault Create(string keyVaultUrl)
    {
        var logger = _loggerFactory.CreateLogger<AzureVault>();
        return new AzureVault(keyVaultUrl, BuildCredential(), logger);
    }

    /// <summary>
    /// Enumerates all secret names in the given Azure Key Vault. Used by the Secret Migrator
    /// (listing isn't part of the runtime <see cref="IVault"/> contract).
    /// </summary>
    public async IAsyncEnumerable<string> ListSecretNamesAsync(
        string keyVaultUrl,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(keyVaultUrl))
            throw new ArgumentException("Key Vault URL cannot be null or empty", nameof(keyVaultUrl));

        var client = new SecretClient(new Uri(keyVaultUrl), BuildCredential());
        await foreach (var props in client.GetPropertiesOfSecretsAsync(ct))
        {
            yield return props.Name;
        }
    }

    private TokenCredential BuildCredential()
    {
        var akv = _secretStoreSettings.AzureKeyVault;
        if (akv.UseExplicitCredentials)
        {
            return new ClientSecretCredential(
                akv.ExplicitCredentials.TenantId,
                akv.ExplicitCredentials.ClientId,
                akv.ExplicitCredentials.ClientSecret);
        }
        return new DefaultAzureCredential(akv.CredentialOptions);
    }
}