// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.IO.Compression;
using Newtonsoft.Json.Linq;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Exceptions;
using SnapCd.Runner.Services.Plan;
using File = System.IO.File;

namespace SnapCd.Runner.Services;

public class TerraformEngine : BaseEngine, IEngine
{
    private readonly string _engine;

    private const string PlanEntryName = "tfplan";
    private const string StateEntryName = "tfstate";

    public TerraformEngine(
        RunnerTaskContext context,
        ILogger logger,
        ModuleDirectoryService moduleDirectoryService,
        string engine,
        List<string> additionalBinaryPaths,
        Dictionary<string, string> runnerEnvVars,
        List<EngineFlagEntry> engineFlags,
        List<EngineArrayFlagEntry> engineArrayFlags
    ) : base(context, logger, moduleDirectoryService, additionalBinaryPaths, runnerEnvVars, engineFlags, engineArrayFlags)
    {
        _engine = engine;
    }

    public IParsedPlan ParseDestroyPlan()
    {
        return ParsePlan(GetPlanDestroyPath());
    }

    public IParsedPlan ParseApplyPlan()
    {
        return ParsePlan(GetPlanApplyPath());
    }

    private TerraformParsedPlan ParsePlan(string planPath)
    {
        using var archive = ZipFile.OpenRead(planPath);
        var entry = archive.GetEntry(PlanEntryName)
                    ?? throw new InvalidDataException($"Missing {PlanEntryName} entry in archive");

        using (var entryStream = entry.Open())
        {
            var plan = Tfplan.Plan.Parser.ParseFrom(entryStream);
            var state = ParseState(archive);
            return new TerraformParsedPlan
            {
                Plan = plan,
                State = state
            };
        }
    }

    private JObject ParseState(ZipArchive archive)
    {
        var entry = archive.GetEntry(StateEntryName)
                    ?? throw new InvalidDataException($"Missing {StateEntryName} entry in archive");

        using (var entryStream = entry.Open())
        using (var reader = new StreamReader(entryStream))
        {
            var content = reader.ReadToEnd();
            return JObject.Parse(content);
        }
    }

    public async Task<string> Init(
        Dictionary<string, string> resolvedEnvVars,
        string? beforeHook,
        string? afterHook,
        EngineBackendConfiguration backendConfig,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        var merged = new Dictionary<string, string>(RunnerEnvVars);
        foreach (var kvp in resolvedEnvVars)
            merged[kvp.Key] = kvp.Value;
        EnvVars = merged;
        SaveEnvVarsToFile();

        var upgradeRequested = EngineFlags.Any(f => f.Flag == "-upgrade");
        var reconfigureRequested = EngineFlags.Any(f => f.Flag == "-reconfigure");
        var migrateRequested = EngineFlags.Any(f => f.Flag == "-migrate-state");

        // Remove init-managed flags from new flag lists to prevent duplication via AppendFlagArgs
        EngineFlags.RemoveAll(f => f.Flag is "-upgrade" or "-reconfigure" or "-migrate-state");
        var backendConfigEntries = EngineArrayFlags.Where(f => f.Flag == "-backend-config").ToList();
        EngineArrayFlags.RemoveAll(f => f.Flag == "-backend-config");

        var initFlags = new List<string>();
        if (upgradeRequested) initFlags.Add("-upgrade");
        if (reconfigureRequested) initFlags.Add("-reconfigure");
        if (migrateRequested) initFlags.Add("-migrate-state");

        var initCommand = $"{_engine} init {string.Join(" ", initFlags)}";
        string baseScript;
        if (migrateRequested)
            baseScript = $"echo \"yes\" | {initCommand}";
        else if (reconfigureRequested)
            baseScript = $"echo \"no\" | {initCommand}";
        else
            baseScript = initCommand;

        var backendConfigArgs = BuildBackendConfigArgs(backendConfigEntries);
        if (!string.IsNullOrWhiteSpace(backendConfigArgs))
            baseScript += $" {backendConfigArgs}";

        baseScript = AppendFlagArgs(baseScript);

        var script = await CreateScriptAsync(
            baseScript,
            beforeHook,
            afterHook,
            killCancellationToken);

        await File.WriteAllTextAsync($"{SnapCdDir}/init.sh", script);

        return await RunProcess(script, killCancellationToken, gracefulCancellationToken);
    }

    private static string BuildBackendConfigArgs(List<EngineArrayFlagEntry> backendConfigEntries)
    {
        var backendConfigs = new Dictionary<string, string>();

        foreach (var entry in backendConfigEntries)
        {
            var eqIndex = entry.Value.IndexOf('=');
            if (eqIndex > 0)
            {
                var key = entry.Value[..eqIndex];
                var value = entry.Value[(eqIndex + 1)..];
                backendConfigs[key] = value;
            }
        }

        var args = new List<string>();
        foreach (var kvp in backendConfigs)
            args.Add($"-backend-config=\"{kvp.Key}={kvp.Value}\"");

        return string.Join(" ", args);
    }

    public async Task Validate(
        string? beforeHook = null,
        string? afterHook = null,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        var baseScript = $"{_engine} validate";

        var script = await CreateScriptAsync(
            baseScript,
            beforeHook,
            afterHook,
            killCancellationToken);

        try
        {
            await RunProcess(script, killCancellationToken, gracefulCancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new EngineValidationException(
                $"{_engine} validation failed in directory {InitDir}",
                ex,
                InitDir,
                -1,
                ex.Message);
        }
    }

    public async Task<int> Statistics(CancellationToken killCancellationToken = default, CancellationToken gracefulCancellationToken = default)
    {
        var resources = await RunProcess($"{_engine} state list", killCancellationToken, gracefulCancellationToken);

        var lines = resources.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !line.Trim().StartsWith("data."))
            .ToArray();
        return lines.Length;
    }

    public async Task<bool> HasNothingToDestroy(CancellationToken killCancellationToken = default, CancellationToken gracefulCancellationToken = default)
    {
        try
        {
            return await Statistics(killCancellationToken, gracefulCancellationToken) == 0;
        }
        catch (ProcessFailedException) when (!killCancellationToken.IsCancellationRequested && !gracefulCancellationToken.IsCancellationRequested)
        {
            // `state list` exits non-zero when no state file exists at all, which is the
            // never-applied module — nothing to destroy.
            return true;
        }
    }

    public async Task<string> Plan(
        Dictionary<string, string> parameters,
        string? planBeforeHook,
        string? planAfterHook,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        var tfVarsString = string.Join("", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}\n"));

        var tfvarsPath = GetTfVarsPath();
        await File.WriteAllTextAsync(tfvarsPath, tfVarsString);

        var planCommand = AppendFlagArgs($"{_engine} plan -out={GetPlanApplyPath()} -input=false -var-file={tfvarsPath}");

        var script = await CreateScriptAsync(
            planCommand,
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
        var tfVarsString = string.Join("", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}\n"));

        var tfvarsPath = GetTfVarsPath();
        await File.WriteAllTextAsync(tfvarsPath, tfVarsString);

        var planDestroyCommand = AppendFlagArgs($"{_engine} plan -destroy -out={GetPlanDestroyPath()} -input=false -var-file={tfvarsPath}");

        var script = await CreateScriptAsync(
            planDestroyCommand,
            beforeHook,
            afterHook,
            killCancellationToken);

        await File.WriteAllTextAsync($"{SnapCdDir}/plan_destroy.sh", script);

        return await RunProcess(script, killCancellationToken, gracefulCancellationToken);
    }

    public async Task<string> DestroyFromPlan(
        string? beforeHook,
        string? afterHook,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        var applyCommand = AppendFlagArgs($"{_engine} apply {GetPlanDestroyPath()}");
        var mainCommand = $"{applyCommand}\n{_engine} state list | grep -v '^data\\.' | wc -l > {SnapCdDir}/statistics.txt";

        var script = await CreateScriptAsync(
            mainCommand,
            beforeHook,
            afterHook,
            killCancellationToken);

        await File.WriteAllTextAsync($"{SnapCdDir}/destroy.sh", script);

        return await RunProcess(script, killCancellationToken, gracefulCancellationToken);
    }

    public async Task<string> ApplyFromPlan(
        string? beforeHook,
        string? afterHook,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        var applyCmd = AppendFlagArgs($"{_engine} apply {GetPlanApplyPath()}");
        var mainCommand = $"{applyCmd}\n{_engine} state list | grep -v '^data\\.' | wc -l > {SnapCdDir}/statistics.txt";

        var script = await CreateScriptAsync(
            mainCommand,
            beforeHook,
            afterHook,
            killCancellationToken);

        await File.WriteAllTextAsync($"{SnapCdDir}/apply.sh", script);

        return await RunProcess(script, killCancellationToken, gracefulCancellationToken);
    }

    public async Task<string> Output(
        string? beforeHook,
        string? afterHook,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        var outputCommand = AppendFlagArgs($"{_engine} output -json");

        var script = await CreateScriptAsync(
            $"{outputCommand} > .snapcd/output.json",
            beforeHook,
            afterHook,
            killCancellationToken);

        await File.WriteAllTextAsync($"{SnapCdDir}/output.sh", script);

        await RunProcess(script, killCancellationToken, gracefulCancellationToken);

        using var reader = new StreamReader($"{SnapCdDir}/output.json");
        var output = reader.ReadToEnd();

        return output;
    }

    private string BuildFlagArgs()
    {
        var args = new List<string>();
        foreach (var f in EngineFlags)
        {
            if (f.Value != null)
                args.Add($"{f.Flag}={f.Value}");
            else
                args.Add(f.Flag);
        }
        foreach (var f in EngineArrayFlags)
        {
            args.Add($"{f.Flag}={f.Value}");
        }
        return string.Join(" ", args);
    }

    private string AppendFlagArgs(string command)
    {
        var flagArgs = BuildFlagArgs();
        if (!string.IsNullOrEmpty(flagArgs))
            command += $" {flagArgs}";
        return command;
    }

    private string GetTfVarsPath() => $"{SnapCdDir}/inputs.tfvars";
    public void SetPolicyPacks(IReadOnlyList<string> packDirs)
    {
        // Terraform/OpenTofu policies are evaluated by the PolicyValidate step, not inside the plan.
        if (packDirs.Count > 0)
            throw new NotSupportedException("Policy packs are a Pulumi/CrossGuard concept and cannot be applied to the Terraform engine.");
    }

    public async Task<string> ExportPlanJson(
        bool isDestroyJob,
        CancellationToken killCancellationToken = default,
        CancellationToken gracefulCancellationToken = default)
    {
        var planPath = isDestroyJob ? GetPlanDestroyPath() : GetPlanApplyPath();
        var jsonPath = isDestroyJob ? $"{SnapCdDir}/destroy_plan.json" : $"{SnapCdDir}/plan.json";

        // Redirect to file: the plan JSON contains sensitive values and must not stream into logs.
        var script = $"{_engine} show -json {planPath} > {jsonPath}";
        await RunProcess(script, killCancellationToken, gracefulCancellationToken);

        return jsonPath;
    }

    private string GetPlanApplyPath() => $"{SnapCdDir}/plan.out";
    private string GetPlanDestroyPath() => $"{SnapCdDir}/destroy.out";
}
