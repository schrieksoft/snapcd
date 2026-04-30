namespace SnapCd.Runner.Configuration;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddExternalConfiguration(
        this IConfigurationBuilder builder)
    {
        return File.Exists("externalsettings.json")
            ? builder.Add(new ExternalConfigurationSource("externalsettings.json"))
            : builder;
    }
}
