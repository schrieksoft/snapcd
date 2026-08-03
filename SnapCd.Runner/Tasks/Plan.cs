// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Runner.Services.PolicyEvaluation;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    public async Task Plan(PlanRequestBase request, HubConnection connection)
    {
        var killCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, killCts, CancellationType.ImmediateKill);

        var gracefulCts = new CancellationTokenSource();
        _processRegistry.Register(request.JobId, gracefulCts, CancellationType.ImmediateGraceful);

        // Start periodic task reporting
        var reportingCts = CancellationTokenSource.CreateLinkedTokenSource(killCts.Token, gracefulCts.Token);
        var reportingTask = StartPeriodicTaskReporting(
            request.JobId,
            nameof(Plan),
            connection,
            TimeSpan.FromSeconds(request.ReportActiveJobFrequencySeconds),
            reportingCts.Token);

        var logger = _loggerFactory.CreateLogger<Tasks>();
        var taskContext = new RunnerTaskContext(
            request.JobId,
            nameof(Plan),
            logger,
            _jobLogStream,
            request.Metadata
        );

        var runnerHubClient = new RunnerHubClient(connection);

        try
        {
            taskContext.LogInformation("Now planning");

            // Validate hooks against pre-approved hooks
            _hookPreapprovalService.ValidateHooks(
                (request.PlanBeforeHook, nameof(request.PlanBeforeHook)),
                (request.PlanAfterHook, nameof(request.PlanAfterHook))
            );

            var engine = _engineFactory.Create(
                taskContext,
                request.Engine,
                request.Metadata,
                request.PulumiFlags,
                request.PulumiArrayFlags,
                request.TerraformFlags,
                request.TerraformArrayFlags
            );

            string planOutput;
            if (request.Policies.Count > 0)
            {
                taskContext.LogInformation($"Enforcing {request.Policies.Count} CrossGuard policy pack(s) in the preview");
                var packScratchDir = Path.Combine(engine.GetSnapCdDir(), "policy-packs");
                if (Directory.Exists(packScratchDir))
                    Directory.Delete(packScratchDir, recursive: true);
                Directory.CreateDirectory(packScratchDir);

                var packDirs = new List<string>();
                foreach (var policy in request.Policies)
                    packDirs.Add(await PulumiPackMaterializer.MaterializeAsync(policy, packScratchDir, _policyEvaluationSettings, killCts.Token));
                engine.SetPolicyPacks(packDirs);
            }
            planOutput = await engine.Plan(request.ResolvedParameters, request.PlanBeforeHook, request.PlanAfterHook, killCts.Token, gracefulCts.Token);

            var plan = engine.ParseApplyPlan();

            // Extract resource counts and changes
            var unchangedResourcesCount = plan.GetResourceCount(PlanAction.Noop);
            var createResourcesCount = plan.GetResourceCount(PlanAction.Create);
            var modifyResourcesCount = plan.GetResourceCount(PlanAction.Update);
            var destroyResourcesCount = plan.GetResourceCount(PlanAction.Delete);
            var recreateResourcesCount = plan.GetResourceCount(PlanAction.Replace);
            var totalChangedResourcesCount = createResourcesCount + modifyResourcesCount + destroyResourcesCount + recreateResourcesCount;
            var totalResourcesCountAfter = unchangedResourcesCount + modifyResourcesCount + recreateResourcesCount + createResourcesCount;
            var totalResourcesCountBefore = unchangedResourcesCount + modifyResourcesCount + recreateResourcesCount + destroyResourcesCount;

            taskContext.LogInformation(
                $"Plan summary:\n- Unchanged: {unchangedResourcesCount}\n- Create:    {createResourcesCount}\n- Modify:    {modifyResourcesCount}\n- Destroy:   {destroyResourcesCount}\n- Recreate:  {recreateResourcesCount}\n- Count Before Apply:  {totalResourcesCountBefore}\n- Count After Apply:   {totalResourcesCountAfter}");

            // Extract output counts and changes
            var unchangedOutputsCount = plan.GetOutputCount(PlanAction.Noop);
            var createOutputsCount = plan.GetOutputCount(PlanAction.Create);
            var modifyOutputsCount = plan.GetOutputCount(PlanAction.Update);
            var destroyOutputsCount = plan.GetOutputCount(PlanAction.Delete);
            var recreateOutputsCount = plan.GetOutputCount(PlanAction.Replace);
            var totalChangedOutputsCount = createOutputsCount + modifyOutputsCount + destroyOutputsCount + recreateOutputsCount;

            taskContext.LogInformation(
                $"Plan summary:\n- Unchanged Outputs: {unchangedOutputsCount}\n- Create Outputs:    {createOutputsCount}\n- Modify Outputs:    {modifyOutputsCount}\n- Destroyed Outputs: {destroyOutputsCount}\n- Recreate Outputs:  {recreateOutputsCount}");

            // Build response data
            var planData = new PlanCompletedData
            {
                PolicyOutcome = request.Policies.Count > 0 ? PulumiPolicyOutputParser.Classify(planOutput) : null,
                TotalCountAfter = totalResourcesCountAfter,
                TotalCountBefore = totalResourcesCountBefore,
                TotalChangedCount = totalChangedResourcesCount,
                TotalUnchangedCount = unchangedResourcesCount,
                CreateCount = createResourcesCount,
                ModifyCount = modifyResourcesCount,
                DestroyCount = destroyResourcesCount,
                RecreateCount = recreateResourcesCount,
                OutputsTotalCount = totalChangedOutputsCount + unchangedOutputsCount,
                OutputsTotalChangedCount = totalChangedOutputsCount,
                OutputsTotalUnchangedCount = unchangedOutputsCount,
                OutputsCreateCount = createOutputsCount,
                OutputsModifyCount = modifyOutputsCount,
                OutputsDestroyCount = destroyOutputsCount,
                OutputsRecreateCount = recreateOutputsCount,
                OutputsUnchangedList = plan.GetOutputChange(PlanAction.Noop).Select(o => o.Name).ToList(),
                OutputsCreateList = plan.GetOutputChange(PlanAction.Create).Select(o => o.Name).ToList(),
                OutputsModifyList = plan.GetOutputChange(PlanAction.Update).Select(o => o.Name).ToList(),
                OutputsDestroyList = plan.GetOutputChange(PlanAction.Delete).Select(o => o.Name).ToList(),
                OutputsRecreateList = plan.GetOutputChange(PlanAction.Replace).Select(o => o.Name).ToList()
            };

            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokePlanCompleted(request.JobId, planData),
                nameof(runnerHubClient.InvokePlanCompleted),
                request.JobId,
                connection);

            taskContext.LogInformation("Completed Plan");
        }
        catch (OperationCanceledException)
        {
            taskContext.LogWarning("Plan process was cancelled.");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokePlanCancelled(request.JobId),
                nameof(runnerHubClient.InvokePlanCancelled),
                request.JobId,
                connection);
        }
        catch (Exception ex)
        {
            taskContext.LogError($"Error handling Plan for job {request.JobId}. {ex.Message}");
            await InvokeWithRetryAsync(
                () => runnerHubClient.InvokePlanFaulted(
                    request.JobId,
                    ex.Message,
                    ex.StackTrace,
                    request.Policies.Count > 0 && ex is SnapCd.Runner.Services.ProcessFailedException pfe
                        ? PulumiPolicyOutputParser.Classify(pfe.Output + pfe.Error)
                        : null
                ),
                nameof(runnerHubClient.InvokePlanFaulted),
                request.JobId,
                connection);
        }
        finally
        {
            // Stop periodic reporting
            reportingCts?.Cancel();
            if (reportingTask != null)
                try
                {
                    await reportingTask;
                }
                catch
                {
                    /* Already logged */
                }

            _processRegistry.Remove(request.JobId, CancellationType.ImmediateKill);
            _processRegistry.Remove(request.JobId, CancellationType.ImmediateGraceful);
        }
    }
}