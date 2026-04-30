using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SnapCd.Runner.Configuration;

public class ExternalConfigurationProvider : ConfigurationProvider
{
    private ExternalConfigurationSource Source { get; }
    private List<IDictionary<string, string>> LoadedData { get; set; }

    public ExternalConfigurationProvider(ExternalConfigurationSource source)
    {
        Source = source;
        LoadedData = new List<IDictionary<string, string>>(new IDictionary<string, string>[Source.Providers.Count]);
    }

    public override void Load()
    {
        for (var i = 0; i < Source.Providers.Count; i++)
        {
            var provider = Source.Providers[i];

            // Use the data loader to load the data from the paths specified in flattenedData

            IDictionary<string, string> loadedData;

            if (LoadedData.ElementAtOrDefault(i) == null)
            {
                // Flatten the input data (e.g. {"foo":{"baz": "bar"}} becomes {"foo:baz": "bar"})
                var flattenedData = provider.FlattenData();

                // Create the data loader
                var loader = DataLoaderFactory.CreateLoader(provider.Loader, provider.Configuration);

                // Use the data loader to load the data from the paths specified in flattenedData
                loadedData = loader.Load(flattenedData);

                // Store loadedData in LoadedData list at the current index
                LoadedData[i] = loadedData;
            }
            else
            {
                loadedData = LoadedData[i];
            }

            // Update the provider's "Data" object (thereby adding each data point to the application's configuration)
            foreach (var kvp in loadedData) Data[kvp.Key] = kvp.Value; // Update the existing entry
        }
    }
}

// Define classes to represent the structure of each item in the list
public class ExternalProvider
{
    public required string Loader { get; set; }
    public required Dictionary<string, string> Configuration { get; set; }
    public required Dictionary<string, object> Data { get; set; }

    public IDictionary<string, string> FlattenData()
    {
        var flattenedJson = new Dictionary<string, string>();
        FlattenJsonHelper(JToken.Parse(JsonConvert.SerializeObject(Data)), string.Empty, flattenedJson);
        return flattenedJson;
    }

    private void FlattenJsonHelper(JToken token, string prefix, Dictionary<string, string> flattenedJson)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                foreach (var property in token.Children<JProperty>()) FlattenJsonHelper(property.Value, $"{prefix}{property.Name}:", flattenedJson);
                break;
            case JTokenType.Array:
                var index = 0;
                foreach (var item in token.Children())
                {
                    FlattenJsonHelper(item, $"{prefix}{index}:", flattenedJson);
                    index++;
                }

                break;
            default:
                flattenedJson[prefix.TrimEnd(':')] = token.ToString();
                break;
        }
    }
}
