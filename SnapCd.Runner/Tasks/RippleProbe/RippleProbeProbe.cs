// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.RunnerRequests.RippleProbe;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    /// <summary>
    /// Plans the Module with an upstream's changed outputs supplied as the values they will become,
    /// and reports what that does to each of its own outputs. Runs planwalker rather than the
    /// engine: tfvars is HCL and cannot express an unknown, so only a caller supplying cty values
    /// directly can ask this question.
    /// </summary>
    public async Task RippleProbeProbe(RippleProbeProbeRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(RippleProbeProbe),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(RippleProbeProbe),
            logger,
            _jobLogStreams.RippleProbes,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

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
                () => runnerHubClient.InvokeRippleProbeProbeCompleted(request.JobId, signalsJson),
                nameof(runnerHubClient.InvokeRippleProbeProbeCompleted),
                request.JobId,
                connection);

            taskContext.LogSection("Completed RippleProbeProbe");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("RippleProbeProbe was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeRippleProbeProbeCancelled(request.JobId),
                nameof(runnerHubClient.InvokeRippleProbeProbeCancelled),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling RippleProbeProbe for job {JobId}", request.JobId);
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokeRippleProbeProbeFaulted(request.JobId, ex.Message, ex.StackTrace),
                nameof(runnerHubClient.InvokeRippleProbeProbeFaulted),
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
