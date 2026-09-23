// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Contracts.RunnerRequests.Transfers;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    /// <summary>
    /// Checks the Module out at the ref its side of the transfer is being proved against, and
    /// reports the commit that ref resolved to, which is what the proof is recorded against.
    /// </summary>
    public Task TransferGetModule(TransferGetModuleRequestBase request, HubConnection connection) =>
        RunTransferStep(request.JobId, request.ModuleId, nameof(TransferGetModule), request.Metadata,
            request.ReportActiveJobFrequencySeconds, connection,
            async (taskContext, client, killToken, gracefulToken) =>
            {
                taskContext.LogNarration("Now cloning repo");

                var moduleGetter = await _moduleGetterFactory.Create(
                    taskContext,
                    request.SourceType,
                    request.SourceRevisionType,
                    request.SourceUrl,
                    request.SourceRevision,
                    request.Metadata,
                    request.Engine);

                await moduleGetter.GetModule(
                    request.CleanInitEnabled, request.ExtraFiles, killToken, gracefulToken);

                var definitiveRevision = await moduleGetter.GetRemoteDefinitiveRevision();

                await InvokeWithRetryAsync(
                    () => client.InvokeTransferGetModuleCompleted(request.JobId, request.ModuleId, definitiveRevision),
                    nameof(client.InvokeTransferGetModuleCompleted), request.JobId, connection);
            },
            (client, message, stackTrace) =>
                client.InvokeTransferGetModuleFaulted(request.JobId, request.ModuleId, message, stackTrace));

    public Task TransferInit(TransferInitRequestBase request, HubConnection connection) =>
        RunTransferStep(request.JobId, request.ModuleId, nameof(TransferInit), request.Metadata,
            request.ReportActiveJobFrequencySeconds, connection,
            async (taskContext, client, killToken, gracefulToken) =>
            {
                _hookPreapprovalService.ValidateHooks(
                    (request.InitBeforeHook, nameof(request.InitBeforeHook)),
                    (request.InitAfterHook, nameof(request.InitAfterHook)));

                var engine = _engineFactory.Create(
                    taskContext,
                    request.Engine,
                    request.Metadata,
                    request.BackendConfiguration.PulumiFlags,
                    request.BackendConfiguration.PulumiArrayFlags,
                    request.BackendConfiguration.TerraformFlags,
                    request.BackendConfiguration.TerraformArrayFlags);

                await engine.Init(
                    request.ResolvedEnvVars, request.InitBeforeHook, request.InitAfterHook,
                    request.BackendConfiguration, killToken, gracefulToken);

                await InvokeWithRetryAsync(
                    () => client.InvokeTransferInitCompleted(request.JobId, request.ModuleId),
                    nameof(client.InvokeTransferInitCompleted), request.JobId, connection);
            },
            (client, message, stackTrace) =>
                client.InvokeTransferInitFaulted(request.JobId, request.ModuleId, message, stackTrace));

    public Task TransferValidate(TransferValidateRequestBase request, HubConnection connection) =>
        RunTransferStep(request.JobId, request.ModuleId, nameof(TransferValidate), request.Metadata,
            request.ReportActiveJobFrequencySeconds, connection,
            async (taskContext, client, killToken, gracefulToken) =>
            {
                _hookPreapprovalService.ValidateHooks(
                    (request.ValidateBeforeHook, nameof(request.ValidateBeforeHook)),
                    (request.ValidateAfterHook, nameof(request.ValidateAfterHook)));

                var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);

                await engine.Validate(
                    request.ValidateBeforeHook, request.ValidateAfterHook, killToken, gracefulToken);

                await InvokeWithRetryAsync(
                    () => client.InvokeTransferValidateCompleted(request.JobId, request.ModuleId),
                    nameof(client.InvokeTransferValidateCompleted), request.JobId, connection);
            },
            (client, message, stackTrace) =>
                client.InvokeTransferValidateFaulted(request.JobId, request.ModuleId, message, stackTrace));

    /// <summary>
    /// Plans the Module and reports only how much would change. A transfer proceeds from a plan
    /// with nothing in it, so the count is the whole answer.
    /// </summary>
    public Task TransferPlan(TransferPlanRequestBase request, HubConnection connection) =>
        RunTransferStep(request.JobId, request.ModuleId, nameof(TransferPlan), request.Metadata,
            request.ReportActiveJobFrequencySeconds, connection,
            async (taskContext, client, killToken, gracefulToken) =>
            {
                _hookPreapprovalService.ValidateHooks(
                    (request.PlanBeforeHook, nameof(request.PlanBeforeHook)),
                    (request.PlanAfterHook, nameof(request.PlanAfterHook)));

                var engine = _engineFactory.Create(
                    taskContext,
                    request.Engine,
                    request.Metadata,
                    request.PulumiFlags,
                    request.PulumiArrayFlags,
                    request.TerraformFlags,
                    request.TerraformArrayFlags);

                await engine.Plan(
                    request.ResolvedParameters, request.PlanBeforeHook, request.PlanAfterHook,
                    killToken, gracefulToken);

                var plan = engine.ParseApplyPlan();

                var totalChangedCount =
                    plan.GetResourceCount(PlanAction.Create) +
                    plan.GetResourceCount(PlanAction.Update) +
                    plan.GetResourceCount(PlanAction.Delete) +
                    plan.GetResourceCount(PlanAction.Replace);

                taskContext.LogInformation($"Plan would change {totalChangedCount} resources.");

                await InvokeWithRetryAsync(
                    () => client.InvokeTransferPlanCompleted(request.JobId, request.ModuleId, totalChangedCount),
                    nameof(client.InvokeTransferPlanCompleted), request.JobId, connection);
            },
            (client, message, stackTrace) =>
                client.InvokeTransferPlanFaulted(request.JobId, request.ModuleId, message, stackTrace));

    /// <summary>
    /// The scaffolding every transfer preamble step shares: cancellation, periodic reporting, and
    /// reporting a fault rather than letting it escape. A cancelled step is a fault to a transfer,
    /// which has no cancelled step of its own.
    /// </summary>
    private async Task RunTransferStep(
        Guid jobId,
        Guid moduleId,
        string task,
        JobMetadata metadata,
        int reportFrequencySeconds,
        HubConnection connection,
        Func<RunnerTaskContext, RunnerHubClient, CancellationToken, CancellationToken, Task> run,
        Func<RunnerHubClient, string?, string?, Task> fault)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(jobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(jobId, gracefulCts, CancellationType.ImmediateGraceful);

        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            jobId, task, connection, TimeSpan.FromSeconds(reportFrequencySeconds), reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(jobId, task, logger, _jobLogStream, metadata);
        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            await run(taskContext, runnerHubClient, killCts.Token, gracefulCts.Token);
            taskContext.LogSection($"Completed {task}");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning($"{task} was cancelled.");
            await InvokeWithRetryAsync(
                () => fault(runnerHubClient, "Cancelled.", null), $"{task}Faulted", jobId, connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Unhandled exception occurred. {ex.Message}");
            logger.LogError(ex, "Error handling {Task} for Module {ModuleId} of job {JobId}", task, moduleId, jobId);

            await InvokeWithRetryAsync(
                () => fault(runnerHubClient, ex.Message, ex.StackTrace), $"{task}Faulted", jobId, connection);
        }
        finally
        {
            reportingCts.Cancel();
            try { await reportingTask; }
            catch { /* Already logged */ }

            _processRegistry.Remove(jobId, CancellationType.ImmediateKill);
            _processRegistry.Remove(jobId, CancellationType.ImmediateGraceful);
        }
    }
}
