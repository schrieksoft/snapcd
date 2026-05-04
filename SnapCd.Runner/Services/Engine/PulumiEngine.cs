using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Contracts.Dto.Outputs;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Factories;
using SnapCd.Runner.Services.Plan;
using File = System.IO.File;

namespace SnapCd.Runner.Services;

public class PulumiEngine : BaseEngine, IEngine
{
    private const string PlanFileName = "plan.json";
    private const string DestroyPreviewFileName = "destroy_preview.json";
    private const string OutputFileName = "output.json";
    private const string ExportFileName = "export.json";

    protected override bool TreatStderrAsError => false;

    public PulumiEngine(
        RunnerTaskContext context,
        ILogger logger,
        ModuleDirectoryService moduleDirectoryService,
        List<string> additionalBinaryPaths,
        List<EngineFlagEntry> engineFlags,
        List<EngineArrayFlagEntry> engineArrayFlags
    ) : base(context, logger, moduleDirectoryService, additionalBinaryPaths, engineFlags, engineArrayFlags)
    {
    }

    public IParsedPlan ParseApplyPlan()
    {
        return ParsePlanFile(GetPlanPath());
    }

    public IParsedPlan ParseDestroyPlan()
    {
        return ParsePlanFile(GetDestroyPreviewPath());
    }

    private PulumiParsedPlan ParsePlanFile(string path)
    {
        var json = File.ReadAllText(path);
        var obj = JObject.Parse(json);
        return new PulumiParsedPlan(obj);
    }

    public async Task<string> Init(
        Dictionary<string, string> resolvedEnvVars,
        string? beforeHook,
        string? afterHook,
        EngineBackendConfiguration backendConfig,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        EnvVars = resolvedEnvVars;
        SaveEnvVarsToFile();

        var initFlags = PulumiFlagConverter.Convert(backendConfig.PulumiFlags);
        var globalArgs = GetGlobalFlagArgs();

        var commands = new List<string>();

        // Build login command from init flags
        var loginCommand = BuildLoginCommand(initFlags, globalArgs);
        if (loginCommand != null)
            commands.Add(loginCommand);

        // Build stack select command from init flags
        var stackSelectCommand = BuildStackSelectCommand(initFlags, globalArgs);
        if (stackSelectCommand != null)
            commands.Add(stackSelectCommand);

        var baseScript = string.Join("\n", commands);

        var script = await CreateScriptAsync(
            baseScript,
            beforeHook,
            afterHook,
            killCancellationToken);

        await File.WriteAllTextAsync($"{SnapCdDir}/init.sh", script);

        return await RunProcess(script, killCancellationToken, gracefulCancellationToken);
    }

    public async Task Validate(
        string? beforeHook = null,
        string? afterHook = null,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        // Pulumi has no direct validate equivalent.
        // Run a preview as a best-effort validation.
        var baseScript = "pulumi preview --non-interactive";

        var script = await CreateScriptAsync(
            baseScript,
            beforeHook,
            afterHook,
            killCancellationToken);

        await RunProcess(script, killCancellationToken, gracefulCancellationToken);
    }

    public async Task<string> Plan(
        Dictionary<string, string> parameters,
        string? planBeforeHook,
        string? planAfterHook,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        await WriteConfigValues(parameters, killCancellationToken, gracefulCancellationToken);

        var planPath = GetPlanPath();
        var phaseArgs = GetFlagArgs();
        var baseScript = $"pulumi preview --save-plan {planPath} --non-interactive{phaseArgs}";

        var script = await CreateScriptAsync(
            baseScript,
            planBeforeHook,
            planAfterHook,
            killCancellationToken);

        await File.WriteAllTextAsync($"{SnapCdDir}/plan.sh", script);

        return await RunProcess(script, killCancellationToken, gracefulCancellationToken);
    }

    public async Task<string> PlanDestroy(
        Dictionary<string, string> parameters,
        string? beforeHook,
        string? afterHook,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        await WriteConfigValues(parameters, killCancellationToken, gracefulCancellationToken);

        var destroyPreviewPath = GetDestroyPreviewPath();
        var phaseArgs = GetFlagArgs();
        var baseScript = $"pulumi destroy --preview-only --json --non-interactive{phaseArgs} > {destroyPreviewPath}";

        var script = await CreateScriptAsync(
            baseScript,
            beforeHook,
            afterHook,
            killCancellationToken);

        await File.WriteAllTextAsync($"{SnapCdDir}/plan_destroy.sh", script);

        var result = await RunProcess(script, killCancellationToken, gracefulCancellationToken);

        LogDestroyPreview(destroyPreviewPath);

        return result;
    }

    public async Task<string> ApplyFromPlan(
        string? beforeHook,
        string? afterHook,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        var exportPath = $"{SnapCdDir}/{ExportFileName}";
        var phaseArgs = GetFlagArgs();
        var mainCommand = $"pulumi up --yes --plan {GetPlanPath()} --non-interactive{phaseArgs}\npulumi stack export > {exportPath}";

        var script = await CreateScriptAsync(
            mainCommand,
            beforeHook,
            afterHook,
            killCancellationToken);

        await File.WriteAllTextAsync($"{SnapCdDir}/apply.sh", script);

        var result = await RunProcess(script, killCancellationToken, gracefulCancellationToken);

        await WriteStatisticsFromExport(exportPath);

        return result;
    }

    public async Task<string> DestroyFromPlan(
        string? beforeHook,
        string? afterHook,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        var exportPath = $"{SnapCdDir}/{ExportFileName}";
        var phaseArgs = GetFlagArgs();
        var mainCommand = $"pulumi destroy --yes --non-interactive{phaseArgs}\npulumi stack export > {exportPath}";

        var script = await CreateScriptAsync(
            mainCommand,
            beforeHook,
            afterHook,
            killCancellationToken);

        await File.WriteAllTextAsync($"{SnapCdDir}/destroy.sh", script);

        var result = await RunProcess(script, killCancellationToken, gracefulCancellationToken);

        await WriteStatisticsFromExport(exportPath);

        return result;
    }

    public async Task<string> Output(
        string? beforeHook,
        string? afterHook,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        var outputPath = $"{SnapCdDir}/{OutputFileName}";
        var phaseArgs = GetFlagArgs();
        var script = await CreateScriptAsync(
            $"pulumi stack output --json{phaseArgs} > {outputPath}",
            beforeHook,
            afterHook,
            killCancellationToken);

        await File.WriteAllTextAsync($"{SnapCdDir}/output.sh", script);

        await RunProcess(script, killCancellationToken, gracefulCancellationToken);

        var rawJson = await File.ReadAllTextAsync(outputPath);
        var rawOutputs = JObject.Parse(rawJson);

        // Pulumi stack output --json returns flat {"key": value} without type/sensitive metadata.
        // Wrap into the format expected by ParseJsonToModuleOutputSet.
        var wrappedOutputs = new JObject();
        foreach (var prop in rawOutputs.Properties())
        {
            wrappedOutputs[prop.Name] = new JObject
            {
                ["value"] = prop.Value,
                ["type"] = "string",
                ["sensitive"] = false
            };
        }

        return wrappedOutputs.ToString(Formatting.None);
    }

    public async Task<int> Statistics(
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        var exportPath = $"{SnapCdDir}/{ExportFileName}";
        await RunProcess(
            $"pulumi stack export > {exportPath}",
            killCancellationToken,
            gracefulCancellationToken);

        return CountResourcesFromExport(exportPath);
    }

    #region Flag resolution

    private string? BuildLoginCommand(List<EngineFlagEntry> initFlags, string globalArgs)
    {
        var hasLoginCloud = initFlags.Any(f => f.Flag == "--login-cloud");
        var hasLoginLocal = initFlags.Any(f => f.Flag == "--login-local");
        var cloudUrlFlag = initFlags.FirstOrDefault(f => f.Flag == "--cloud-url");

        string? baseLoginCmd;
        if (hasLoginCloud)
            baseLoginCmd = "pulumi login";
        else if (hasLoginLocal)
            baseLoginCmd = "pulumi login --local";
        else if (cloudUrlFlag != null && !string.IsNullOrWhiteSpace(cloudUrlFlag.Value))
            baseLoginCmd = $"pulumi login {cloudUrlFlag.Value}";
        else
            return null;

        // Append additional login-related init flags
        var loginExtraFlags = new List<string>();
        foreach (var f in initFlags)
        {
            switch (f.Flag)
            {
                case "--default-org":
                    loginExtraFlags.Add(f.Value != null ? $"--default-org {f.Value}" : "--default-org");
                    break;
                case "--insecure":
                    loginExtraFlags.Add("--insecure");
                    break;
            }
        }

        var extra = string.Join(" ", loginExtraFlags);
        if (!string.IsNullOrEmpty(extra))
            baseLoginCmd += $" {extra}";
        if (!string.IsNullOrEmpty(globalArgs))
            baseLoginCmd += globalArgs;

        return baseLoginCmd;
    }

    private string? BuildStackSelectCommand(List<EngineFlagEntry> initFlags, string globalArgs)
    {
        var stackNameFlag = initFlags.FirstOrDefault(f => f.Flag == "--stack-name");
        var stackName = stackNameFlag?.Value;
        if (string.IsNullOrWhiteSpace(stackName))
            stackName = "snapcd";

        var cmd = $"pulumi stack select {stackName}";

        var stackExtraFlags = new List<string>();
        stackExtraFlags.Add("--create");
        var secretsProvider = initFlags.FirstOrDefault(f => f.Flag == "--secrets-provider");
        if (secretsProvider?.Value != null)
            stackExtraFlags.Add($"--secrets-provider {secretsProvider.Value}");

        stackExtraFlags.Add("--non-interactive");

        cmd += $" {string.Join(" ", stackExtraFlags)}";
        if (!string.IsNullOrEmpty(globalArgs))
            cmd += globalArgs;

        return cmd;
    }

    private string GetFlagArgs()
    {
        var args = new List<string>();

        foreach (var f in EngineFlags)
        {
            if (f.Value != null)
                args.Add($"{f.Flag} {f.Value}");
            else
                args.Add(f.Flag);
        }

        foreach (var f in EngineArrayFlags)
            args.Add($"{f.Flag} {f.Value}");

        return args.Count > 0 ? " " + string.Join(" ", args) : "";
    }

    private string GetGlobalFlagArgs()
    {
        // During Init, the engine's own flags are the init-phase flags.
        // Global flags are also included in the init flags list by the server.
        // Filter to only known global flags.
        var globalFlagNames = new HashSet<string> { "--color", "--verbose", "--emoji" };
        var args = new List<string>();

        foreach (var f in EngineFlags.Where(f => globalFlagNames.Contains(f.Flag)))
        {
            if (f.Value != null)
                args.Add($"{f.Flag} {f.Value}");
            else
                args.Add(f.Flag);
        }

        foreach (var f in EngineArrayFlags.Where(f => globalFlagNames.Contains(f.Flag)))
            args.Add($"{f.Flag} {f.Value}");

        return args.Count > 0 ? " " + string.Join(" ", args) : "";
    }

    #endregion

    #region Helpers

    private async Task WriteStatisticsFromExport(string exportPath)
    {
        var count = CountResourcesFromExport(exportPath);
        await File.WriteAllTextAsync($"{SnapCdDir}/statistics.txt", count.ToString());
    }

    private int CountResourcesFromExport(string exportPath)
    {
        if (!File.Exists(exportPath)) return 0;

        var json = File.ReadAllText(exportPath);
        var export = JObject.Parse(json);
        var resources = export["deployment"]?["resources"] as JArray;
        if (resources == null) return 0;

        return resources.Count(r => r["type"]?.ToString() != "pulumi:pulumi:Stack");
    }

    private async Task WriteConfigValues(
        Dictionary<string, string> parameters,
        CancellationToken killCancellationToken,
        CancellationToken gracefulCancellationToken)
    {
        if (parameters.Count == 0) return;

        var configCommands = new List<string>();
        foreach (var kvp in parameters)
            configCommands.Add($"pulumi config set {kvp.Key} {kvp.Value} --non-interactive");

        var configScript = string.Join("\n", configCommands);
        await File.WriteAllTextAsync($"{SnapCdDir}/config.sh", configScript);
        await RunProcess(configScript, killCancellationToken, gracefulCancellationToken);
    }

    private void LogDestroyPreview(string previewPath)
    {
        if (!File.Exists(previewPath)) return;

        var json = File.ReadAllText(previewPath);
        var preview = JObject.Parse(json);

        if (preview["steps"] is JArray steps)
        {
            foreach (var step in steps)
            {
                var op = step["op"]?.ToString() ?? "";
                var urn = step["urn"]?.ToString() ?? "";
                var type = step["newState"]?["type"]?.ToString()
                           ?? step["oldState"]?["type"]?.ToString()
                           ?? "";

                if (type == "pulumi:pulumi:Stack") continue;

                var name = urn.Contains("::") ? urn.Split("::").Last() : urn;
                Context.LogInformation($"  {op,-10} {type} ({name})");
            }
        }

        if (preview["changeSummary"] is JObject summary)
        {
            var parts = summary.Properties()
                .Where(p => p.Value.Value<int>() > 0)
                .Select(p => $"{p.Value} to {p.Name}");
            Context.LogInformation($"Resources: {string.Join(", ", parts)}");
        }
    }

    private string GetPlanPath() => $"{SnapCdDir}/{PlanFileName}";
    private string GetDestroyPreviewPath() => $"{SnapCdDir}/{DestroyPreviewFileName}";

    #endregion
}
