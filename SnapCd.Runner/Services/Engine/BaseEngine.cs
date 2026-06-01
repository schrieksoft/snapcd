// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto;
using SnapCd.Contracts.Dto.Outputs;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Utils;
using File = System.IO.File;

namespace SnapCd.Runner.Services;

public static class NativeMethods
{
    [DllImport("libc", SetLastError = true)]
    public static extern int kill(int pid, int sig);

    public const int Sigint = 2;
}

public abstract class BaseEngine
{
    protected readonly RunnerTaskContext Context;
    protected readonly ILogger Logger;
    protected readonly string SnapCdDir;
    protected readonly string InitDir;
    protected Dictionary<string, string> EnvVars = new();
    private readonly List<string> _additionalBinaryPaths;
    protected readonly List<EngineFlagEntry> EngineFlags;
    protected readonly List<EngineArrayFlagEntry> EngineArrayFlags;
    protected virtual bool TreatStderrAsError => true;

    protected BaseEngine(
        RunnerTaskContext context,
        ILogger logger,
        ModuleDirectoryService moduleDirectoryService,
        List<string> additionalBinaryPaths,
        List<EngineFlagEntry> engineFlags,
        List<EngineArrayFlagEntry> engineArrayFlags)
    {
        Logger = logger;
        Context = context;
        SnapCdDir = moduleDirectoryService.GetSnapCdDir();
        InitDir = moduleDirectoryService.GetInitDir();
        _additionalBinaryPaths = additionalBinaryPaths;
        EngineFlags = engineFlags;
        EngineArrayFlags = engineArrayFlags;

        LoadEnvVarsFromFile();
    }

    public string GetInitDir() => InitDir;
    public string GetSnapCdDir() => SnapCdDir;

    public async Task<string> RunProcess(string script, CancellationToken killCancellationToken, CancellationToken gracefulCancellationToken)
    {
        EnsureEnvVarsLoaded();

        var arguments = $"-c \"{StringFormatting.EscapeBashScript(script)}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = InitDir
        };

        foreach (var envVar in EnvVars)
            startInfo.EnvironmentVariables[envVar.Key] = envVar.Value;

        if (_additionalBinaryPaths.Count > 0)
        {
            var extra = string.Join(":", _additionalBinaryPaths);
            var currentPath = startInfo.EnvironmentVariables["PATH"] ?? "";
            startInfo.EnvironmentVariables["PATH"] = $"{extra}:{currentPath}";
        }

        var process = new Process { StartInfo = startInfo };
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        gracefulCancellationToken.Register(() =>
        {
            if (!process.HasExited)
            {
                var result = NativeMethods.kill(process.Id, NativeMethods.Sigint);
                if (result == 0)
                    Context.LogInformation("Sent SIGINT to process for graceful termination.");
                else
                    Context.LogError("Failed to send SIGINT. Process might already be terminated or access is denied.");
            }
        });

        killCancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                    Context.LogInformation($"Process killed via cancellation.");
                }
            }
            catch (Exception ex)
            {
                Context.LogError($"Error during process kill: {ex.Message}");
            }
        });

        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Context.LogInformation(e.Data);
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                   killCancellationToken,
                   gracefulCancellationToken))
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }

        var error = errorBuilder.ToString();
        if (process.ExitCode != 0 || (TreatStderrAsError && error != ""))
        {
            throw new Exception($"Process in {InitDir} failed. \n {error}");
        }

        return outputBuilder.ToString();
    }

    public async Task<string> CreateScriptAsync(string baseScript, string? beforeHook, string? afterHook, CancellationToken cancellationToken = default)
    {
        var beforeHookMessage = "Now running before hook";
        if (string.IsNullOrEmpty(beforeHook))
            beforeHookMessage = "No before hook defined. Skipping.";

        var afterHookMessage = "Now running after hook";
        if (string.IsNullOrEmpty(afterHook))
            afterHookMessage = "No after hook defined. Skipping.";

        var script = @$"
echo "">>>>>>>> {beforeHookMessage} <<<<<<<<<""
{beforeHook}

echo "">>>>>>>> Now running main script <<<<<<<<<""
{baseScript}

echo "">>>>>>>> {afterHookMessage} <<<<<<<<<""
{afterHook}
";
        return script;
    }

    public async Task<int> ReadStatisticsFromFile()
    {
        var statisticsFilePath = $"{SnapCdDir}/statistics.txt";

        if (!File.Exists(statisticsFilePath))
        {
            Context.LogWarning("Statistics file not found at {Path}", statisticsFilePath);
            return 0;
        }

        try
        {
            var content = await File.ReadAllTextAsync(statisticsFilePath);
            if (int.TryParse(content.Trim(), out var count)) return count;

            Context.LogWarning("Unable to parse statistics from file: {Content}", content);
            return 0;
        }
        catch (Exception ex)
        {
            Context.LogError("Error reading statistics file: {Error}", ex.Message);
            return 0;
        }
    }

    protected string CalculateChecksum(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            foreach (var b in hashBytes) sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
    }

    public virtual async Task<OutputSetCreateDto?> ParseJsonToModuleOutputSet(string json, Dictionary<string, bool>? outputSources = null)
    {
        var jsonObject = JObject.Parse(json);

        var moduleOutputSet = new OutputSetCreateDto
        {
            Checksum = CalculateChecksum(json),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Outputs = new List<OutputCreateDto>()
        };

        var exceptions = new List<Exception>();

        await Task.WhenAll(jsonObject.Properties().Select(property => Task.Run(() =>
        {
            try
            {
                string? type = null;
                if (property.Value["type"] is JArray typeArray && typeArray.Count > 0)
                    type = typeArray[0].ToString();
                else if (property.Value["type"] != null) type = property.Value["type"]?.ToString();

                string? value = null;
                if (property.Value["value"] is JObject valueJObject)
                    value = valueJObject.ToString(Formatting.None);
                else if (property.Value["value"] != null) value = property.Value["value"]?.ToString();

                var fromExtraFile = outputSources != null &&
                                    outputSources.TryGetValue(property.Name, out var isExtra) &&
                                    isExtra;

                var moduleOutput = new OutputReadDto
                {
                    Name = property.Name,
                    Sensitive = property.Value["sensitive"]?.Value<bool>(),
                    Type = type ?? throw new InvalidOperationException("Output type is not defined"),
                    Value = value ?? throw new InvalidOperationException("Output value is not defined"),
                    FromExtraFile = fromExtraFile
                };

                lock (moduleOutputSet.Outputs)
                {
                    moduleOutputSet.Outputs.Add(moduleOutput);
                }
            }
            catch (Exception ex)
            {
                var e = new Exception(
                    $"Error processing JSON property '{property.Name}'", ex);
                exceptions.Add(e);
                Context.LogError($"Error processing JSON property '{property.Name}'. Error: \n {ex}");
            }
        })));

        if (exceptions.Any())
            throw new AggregateException("One or more errors occurred while processing JSON properties.", exceptions);

        return moduleOutputSet;
    }

    protected void SaveEnvVarsToFile()
    {
        var exportLines = EnvVars
            .Select(envVar => $"export {envVar.Key}={System.Text.Json.JsonSerializer.Serialize(envVar.Value)}");

        File.WriteAllText($"{SnapCdDir}/snapcd.env", string.Join(Environment.NewLine, exportLines));

        Context.LogInformation("Environment Variables saved to file");
    }

    protected bool LoadEnvVarsFromFile()
    {
        var envFilePath = $"{SnapCdDir}/snapcd.env";
        if (!File.Exists(envFilePath)) return false;

        try
        {
            var lines = File.ReadAllLines(envFilePath);
            EnvVars = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("export "))
                    continue;

                var exportLine = line.Substring("export ".Length);
                var equalsIndex = exportLine.IndexOf('=');
                if (equalsIndex < 0)
                    continue;

                var key = exportLine.Substring(0, equalsIndex);
                var valueJson = exportLine.Substring(equalsIndex + 1);

                var value = System.Text.Json.JsonSerializer.Deserialize<string>(valueJson);
                if (value != null) EnvVars[key] = value;
            }

            Context.LogInformation("Environment Variables loaded from file");
            return true;
        }
        catch (Exception ex)
        {
            Context.LogWarning($"Failed to load environment variables from file: {ex.Message}");
            return false;
        }
    }

    protected void EnsureEnvVarsLoaded()
    {
        if (EnvVars.Count > 0) return;

        if (!LoadEnvVarsFromFile())
            throw new InvalidOperationException(
                "Environment variables file not found. Init must be run first to resolve and store environment variables.");
    }
}
