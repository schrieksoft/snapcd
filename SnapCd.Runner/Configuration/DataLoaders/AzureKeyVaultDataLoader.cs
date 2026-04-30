using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace SnapCd.Runner.Configuration.DataLoaders;

public class AzureKeyVaultDataLoaderConfiguration
{
    public required string VaultUrl { get; set; }
}

public class AzureKeyVaultDataLoader : IDataLoader
{
    private readonly AzureKeyVaultDataLoaderConfiguration _configuration;

    public AzureKeyVaultDataLoader(AzureKeyVaultDataLoaderConfiguration configuration)
    {
        _configuration = configuration;
    }


    public IDictionary<string, string> Load(IDictionary<string, string> input)
    {
        var output = new Dictionary<string, string>();

        var options = new SecretClientOptions
        {
            Retry =
            {
                Delay = TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromSeconds(16),
                MaxRetries = 5,
                Mode = RetryMode.Exponential
            }
        };

        var client = new SecretClient(new Uri(_configuration.VaultUrl), new DefaultAzureCredential(), options);

        foreach (var kvp in input)
        {
            var (name, version) = ParseSecretReference(kvp.Value);
            var secret = client.GetSecret(name, version).Value.Value;
            output[kvp.Key] = secret;
        }

        return output;
    }

    private static (string? name, string? version) ParseSecretReference(string reference)
    {
        // Parses a reference to a secret into "name" and (if present) "version" components
        // Examples:
        //     "some-name/d023e916822d4dc18c03e924cdc7c285":
        //     (name: "some-name", version: "d023e916822d4dc18c03e924cdc7c285")
        //
        //     "some-name":
        //     (name: "some-name", version: null)

        var slashIndex = reference.IndexOf('/');
        if (slashIndex >= 0)
        {
            var name = reference.Substring(0, slashIndex);
            var version = reference.Substring(slashIndex + 1);
            return (name, version);
        }
        else
        {
            return (reference, null);
        }
    }
}
