namespace SnapCd.Server.Core.Misc.Configuration;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddExternalConfiguration(
        this IConfigurationBuilder builder)
    {
        return File.Exists("externalsettings.json")
            ? builder.Add(new ExternalConfigurationSource("externalsettings.json"))
            : builder;
    }

    /// <summary>
    /// Adds predefined configuration values that are generated at startup.
    /// Currently sets Server:InstanceId to a unique GUID for this server instance.
    /// </summary>
    public static IConfigurationBuilder AddPredefined(
        this IConfigurationBuilder builder)
    {
        var predefinedValues = new Dictionary<string, string?>
        {
            ["Server:InstanceId"] = Guid.NewGuid().ToString()
        };

        return builder.AddInMemoryCollection(predefinedValues);
    }
}