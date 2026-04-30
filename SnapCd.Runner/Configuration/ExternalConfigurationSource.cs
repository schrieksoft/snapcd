using Newtonsoft.Json;

namespace SnapCd.Runner.Configuration;

public class ExternalConfigurationSource : IConfigurationSource
{
    public List<ExternalProvider> Providers { get; init; }


    public ExternalConfigurationSource(string settingsFilePath)
    {
        // Read the JSON file
        var jsonString = File.ReadAllText(settingsFilePath);
        var settings = JsonConvert.DeserializeObject<SettingsFile>(jsonString);

        if (settings == null)
            throw new InvalidOperationException($"Failed to deserialize configuration file '{settingsFilePath}'. The file may be empty or contain invalid JSON.");

        Providers = settings.Providers;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new ExternalConfigurationProvider(this);
    }
}

public class SettingsFile
{
    public required List<ExternalProvider> Providers { get; set; }
}
