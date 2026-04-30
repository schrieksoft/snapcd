using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace SnapCd.Runner.Configuration.DataLoaders;

public class AmazonSecretsManagerDataLoaderConfiguration
{
    public string Region { get; set; } = null!;
}

public class AmazonSecretsManagerDataLoader : IDataLoader
{
    private readonly AmazonSecretsManagerDataLoaderConfiguration _configuration;

    public AmazonSecretsManagerDataLoader(AmazonSecretsManagerDataLoaderConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDictionary<string, string> Load(IDictionary<string, string> input)
    {
        var output = new Dictionary<string, string>();

        var clientConfig = new AmazonSecretsManagerConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_configuration.Region)
        };

        using var client = new AmazonSecretsManagerClient(clientConfig);

        foreach (var kvp in input)
        {
            var (secretId, versionId) = ParseSecretReference(kvp.Value);

            var request = new GetSecretValueRequest
            {
                SecretId = secretId
            };

            if (!string.IsNullOrEmpty(versionId)) request.VersionId = versionId;

            var response = client.GetSecretValueAsync(request).Result;
            output[kvp.Key] = response.SecretString;
        }

        return output;
    }

    private static (string secretId, string? versionId) ParseSecretReference(string reference)
    {
        // Parses a reference to a secret into "secretId" and (if present) "versionId" components
        // Examples:
        //     "my-secret/d023e916-822d-4dc1-8c03-e924cdc7c285":
        //     (secretId: "my-secret", versionId: "d023e916-822d-4dc1-8c03-e924cdc7c285")
        //
        //     "my-secret":
        //     (secretId: "my-secret", versionId: null)

        if (string.IsNullOrEmpty(reference)) throw new ArgumentException("Secret reference cannot be null or empty", nameof(reference));

        var slashIndex = reference.IndexOf('/');
        if (slashIndex >= 0)
        {
            var secretId = reference.Substring(0, slashIndex);
            var versionId = reference.Substring(slashIndex + 1);
            return (secretId, versionId);
        }
        else
        {
            return (reference, null);
        }
    }
}
