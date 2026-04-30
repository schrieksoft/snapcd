using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Runner.Factories;

public static class PulumiFlagConverter
{
    public static List<EngineFlagEntry> Convert(List<PulumiFlagEntry> flags)
    {
        var result = new List<EngineFlagEntry>();
        foreach (var f in flags)
        {
            var cliFlag = MapFlagToCli(f.Flag);
            if (cliFlag != null)
                result.Add(new EngineFlagEntry { Flag = cliFlag, Value = f.Value });
        }
        return result;
    }

    public static List<EngineArrayFlagEntry> Convert(List<PulumiArrayFlagEntry> flags)
    {
        var result = new List<EngineArrayFlagEntry>();
        foreach (var f in flags)
        {
            var cliFlag = MapArrayFlagToCli(f.Flag);
            if (cliFlag != null)
                result.Add(new EngineArrayFlagEntry { Flag = cliFlag, Value = f.Value });
        }
        return result;
    }

    private static string? MapFlagToCli(PulumiFlag flag) => flag switch
    {
        // Init flags
        PulumiFlag.CloudUrl => "--cloud-url",
        PulumiFlag.LoginLocal => "--login-local",
        PulumiFlag.LoginCloud => "--login-cloud",
        PulumiFlag.DefaultOrg => "--default-org",
        PulumiFlag.Insecure => "--insecure",
        PulumiFlag.StackName => "--stack-name",
        PulumiFlag.SecretsProvider => "--secrets-provider",
        PulumiFlag.CreateStack => "--create-stack",
        PulumiFlag.OidcExpiration => "--oidc-expiration",
        PulumiFlag.OidcOrg => "--oidc-org",
        PulumiFlag.OidcTeam => "--oidc-team",
        PulumiFlag.OidcToken => "--oidc-token",
        PulumiFlag.OidcUser => "--oidc-user",

        // Plan/Apply/Destroy shared
        PulumiFlag.ConfigFile => "--config-file",
        PulumiFlag.Debug => "--debug",
        PulumiFlag.Diff => "--diff",
        PulumiFlag.ExpectNoChanges => "--expect-no-changes",
        PulumiFlag.Json => "--json",
        PulumiFlag.Message => "--message",
        PulumiFlag.Parallel => "--parallel",
        PulumiFlag.Refresh => "--refresh",
        PulumiFlag.RunProgram => "--run-program",
        PulumiFlag.ShowConfig => "--show-config",
        PulumiFlag.ShowFullOutput => "--show-full-output",
        PulumiFlag.ShowReads => "--show-reads",
        PulumiFlag.ShowReplacementSteps => "--show-replacement-steps",
        PulumiFlag.ShowSames => "--show-sames",
        PulumiFlag.ShowSecrets => "--show-secrets",
        PulumiFlag.SuppressOutputs => "--suppress-outputs",
        PulumiFlag.SuppressProgress => "--suppress-progress",
        PulumiFlag.TargetDependents => "--target-dependents",
        PulumiFlag.ExcludeDependents => "--exclude-dependents",
        PulumiFlag.Neo => "--neo",

        // Plan only
        PulumiFlag.ImportFile => "--import-file",

        // Apply only
        PulumiFlag.ContinueOnError => "--continue-on-error",
        PulumiFlag.SkipPreview => "--skip-preview",
        PulumiFlag.Strict => "--strict",

        // Destroy only
        PulumiFlag.ExcludeProtected => "--exclude-protected",
        PulumiFlag.Remove => "--remove",

        // Output
        PulumiFlag.Shell => "--shell",

        // Global
        PulumiFlag.Color => "--color",
        PulumiFlag.Verbose => "--verbose",
        PulumiFlag.Emoji => "--emoji",

        _ => null
    };

    private static string? MapArrayFlagToCli(PulumiArrayFlag flag) => flag switch
    {
        PulumiArrayFlag.PolicyPack => "--policy-pack",
        PulumiArrayFlag.PolicyPackConfig => "--policy-pack-config",
        PulumiArrayFlag.AttachDebugger => "--attach-debugger",
        PulumiArrayFlag.Target => "--target",
        PulumiArrayFlag.Replace => "--replace",
        PulumiArrayFlag.Exclude => "--exclude",
        PulumiArrayFlag.TargetReplace => "--target-replace",
        PulumiArrayFlag.Config => "--config",
        _ => null
    };
}
