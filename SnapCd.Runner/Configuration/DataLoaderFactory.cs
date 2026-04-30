using Newtonsoft.Json;
using SnapCd.Runner.Configuration.DataLoaders;

namespace SnapCd.Runner.Configuration;

public static class DataLoaderFactory
{
    public static IDataLoader CreateLoader(string name, Dictionary<string, string> configuration)
    {
        switch (name)
        {
            case "AzureKeyVault":
                var akvDataLoaderConfig =
                    JsonConvert.DeserializeObject<AzureKeyVaultDataLoaderConfiguration>(JsonConvert.SerializeObject(configuration));
                if (akvDataLoaderConfig == null)
                    throw new InvalidOperationException("Failed to deserialize AzureKeyVault configuration. Configuration is required.");
                return new AzureKeyVaultDataLoader(akvDataLoaderConfig);
            case "AmazonSecretsManager":
                var asmDataLoaderConfig =
                    JsonConvert.DeserializeObject<AmazonSecretsManagerDataLoaderConfiguration>(JsonConvert.SerializeObject(configuration));
                if (asmDataLoaderConfig == null)
                    throw new InvalidOperationException("Failed to deserialize AmazonSecretsManager configuration. Configuration is required.");
                return new AmazonSecretsManagerDataLoader(asmDataLoaderConfig);
            case "Literal":
                return new LiteralDataLoader();
            default:
                throw new NotImplementedException(
                    $"A data loader with name {name} has not been implemented. Valid loader names are \"AzureKeyVault\", \"AmazonSecretsManager\", and \"Literal\"");
        }
    }
}
