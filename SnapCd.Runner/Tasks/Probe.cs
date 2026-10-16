// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts;
using SnapCd.Runner.Logging;
using SnapCd.Contracts.RunnerRequests;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    /// <summary>
    /// Plans the Module with an upstream's changed outputs supplied as the values they will become,
    /// and reports what that does to each of its own outputs. Runs planwalker rather than the
    /// engine: tfvars is HCL and cannot express an unknown, so only a caller supplying cty values
    /// directly can ask this question.
    ///
    /// The directory is the one this job already inited, so there is no checkout and no init here.
    /// </summary>
    public async Task Probe(
        ProbeRequestBase request,
        HubConnection connection,
        IJobLogStream logStream,
        Func<Guid, string?, Task> completed,
        Func<Guid, Task> cancelled,
        Func<Guid, string?, string?, Task> faulted)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(Probe),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(Probe),
            logger,
            logStream,
            request.Metadata
        );

        try
        {
            taskContext.LogNarration("Now probing the downstream effect");

            var engine = _engineFactory.Create(
                taskContext,
                request.Engine,
                request.Metadata
            );

            var initDir = engine.GetInitDir();
            var inputsPath = Path.Combine(engine.GetSnapCdDir(), "inputs.json");
            var signalsPath = Path.Combine(engine.GetSnapCdDir(), "signals.json");

            await File.WriteAllTextAsync(inputsPath, request.InputsJson, killCts.Token);

            // planwalker reads the inited directory and writes the signals, and never touches
            // state: it refreshes a state manager and reads the snapshot.
            var command = $"planwalker --dir \"{initDir}\" --inputs \"{inputsPath}\" --out \"{signalsPath}\" --no-color";
            await engine.RunProcess(command, killCts.Token);

            var signalsJson = File.Exists(signalsPath)
                ? await File.ReadAllTextAsync(signalsPath, killCts.Token)
                : null;

            await InvokeWithRetryAsync(
                () => completed(request.JobId, signalsJson),
                "ProbeCompleted",
                request.JobId,
                connection);

            taskContext.LogSection("Completed Probe");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("Probe process was cancelled.");
            await InvokeWithRetryAsync(
                () => cancelled(request.JobId),
                "ProbeCancelled",
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            await InvokeWithRetryAsync(
                () => faulted(request.JobId, ex.Message, ex.StackTrace),
                "ProbeFaulted",
                request.JobId,
                connection);
        }
        finally
        {
            reportingCts?.Cancel();
            if (reportingTask != null)
            {
                try { await reportingTask; }
                catch { /* Already logged */ }
            }

            _processRegistry.Remove(request.JobId, CancellationType.ImmediateKill);
        }
    }
}
