using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Runner.Factories;

public static class TerraformFlagConverter
{
    public static List<EngineFlagEntry> Convert(List<TerraformFlagEntry> flags)
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

    public static List<EngineArrayFlagEntry> Convert(List<TerraformArrayFlagEntry> flags)
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

    private static string? MapFlagToCli(TerraformFlag flag) => flag switch
    {
        // Init
        TerraformFlag.ForceCopy => "-force-copy",
        TerraformFlag.FromModule => "-from-module",
        TerraformFlag.GetPlugins => "-get-plugins",
        TerraformFlag.LockTimeout => "-lock-timeout",
        TerraformFlag.Lockfile => "-lockfile",
        TerraformFlag.MigrateState => "-migrate-state",
        TerraformFlag.Plugin => "-plugin",
        TerraformFlag.Reconfigure => "-reconfigure",
        TerraformFlag.TestDirectory => "-test-directory",
        TerraformFlag.Upgrade => "-upgrade",

        // Plan/Apply/Destroy shared
        TerraformFlag.CompactWarnings => "-compact-warnings",
        TerraformFlag.Concurrency => "-concurrency",
        TerraformFlag.Lock => "-lock",
        TerraformFlag.NoColor => "-no-color",
        TerraformFlag.Parallelism => "-parallelism",
        TerraformFlag.Refresh => "-refresh",
        TerraformFlag.RefreshOnly => "-refresh-only",

        // Plan only
        TerraformFlag.DetailedExitcode => "-detailed-exitcode",
        TerraformFlag.GenerateConfigOut => "-generate-config-out",

        // Apply only
        TerraformFlag.CreateBeforeDestroy => "-create-before-destroy",

        // Output only
        TerraformFlag.Raw => "-raw",

        _ => null
    };

    private static string? MapArrayFlagToCli(TerraformArrayFlag flag) => flag switch
    {
        TerraformArrayFlag.Target => "-target",
        TerraformArrayFlag.Replace => "-replace",
        TerraformArrayFlag.Exclude => "-exclude",
        TerraformArrayFlag.Var => "-var",
        TerraformArrayFlag.BackendConfig => "-backend-config",
        _ => null
    };
}
