// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Factories;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.Consumers.Tasks;

/// <summary>
/// Server-side consumer that receives Plan requests and dispatches them to runners via SignalR.
/// Now resolves Terraform variables on the server before dispatching to eliminate circular API calls.
/// </summary>
public class PlanConsumer : IConsumer<PlanRequested>
{
    private readonly ILogger<PlanConsumer> _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerSelectionService _runnerSelection;
    private readonly ParamResolverFactory _paramResolverFactory;

    public PlanConsumer(
        ILogger<PlanConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection,
        ParamResolverFactory paramResolverFactory)
    {
        _logger = logger;
        _hubContext = hubContext;
        _runnerSelection = runnerSelection;
        _paramResolverFactory = paramResolverFactory;
    }

    public async Task Consume(ConsumeContext<PlanRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;

        var orgId = msg.OrganizationId;
        var runnerId = msg.RunnerId;
        var specificRunner = msg.RunnerInstanceName;

        _logger.LogDebug("Received Plan request for job {JobId} in pool {RunnerId}",
            jobId, runnerId);

        try
        {
            // Resolve Terraform variables on the server before dispatching
            _logger.LogDebug("Resolving Terraform variables for job {JobId}", jobId);

            var metadata = new JobMetadata
            {
                ModuleName = msg.Declared.ModuleName,
                NamespaceName = msg.Declared.NamespaceName,
                StackName = msg.Declared.StackName,
                ModuleId = msg.Declared.ModuleId,
                SourceSubdirectory = msg.Declared.SourceSubdirectory
            };
            
            var taskContext = new ServerTaskContext(
                jobId,
                "Plan",
                _logger,
                metadata
            );

            var paramResolver = _paramResolverFactory.CreateForParams(
                taskContext,
                msg.Declared.ModuleParamFromDefinitions ?? [],
                msg.Declared.ModuleParamFromLiterals ?? [],
                msg.Declared.ModuleParamFromNamespaces ?? [],
                msg.Declared.NamespaceParamFromLiterals ?? [],
                msg.Declared.NamespaceParamFromDefinitions ?? [],
                msg.Declared.SelectedModuleParamsFromSecrets,
                msg.Declared.SelectedNamespaceParamsFromSecrets,
                msg.Declared.StackId,
                msg.Declared.StackName,
                msg.Declared.NamespaceId,
                msg.Declared.NamespaceName,
                msg.Declared.ModuleId,
                msg.Declared.ModuleName,
                msg.Declared.SourceRevision,
                msg.Declared.SourceUrl,
                msg.Declared.SourceSubdirectory,
                orgId,
                msg.Declared.Engine
            );

            var resolvedParameters = await paramResolver.ResolveParameters();

            _logger.LogDebug("Resolved {Count} Terraform variables for job {JobId}",
                resolvedParameters.Count, jobId);

            // Select runner using least-loaded strategy
            var runner = await _runnerSelection.SelectSpecificRunnerAsync(orgId, runnerId, specificRunner);

            if (runner == null)
            {
                _logger.LogWarning("No available runners in pool {RunnerId} for job {JobId}", runnerId, jobId);
                throw new InvalidOperationException($"No available runners in pool {runnerId}");
            }

            _logger.LogDebug("Selected runner {RunnerName} (ConnectionId: {ConnectionId}) for job {JobId}",
                runner.InstanceName, runner.SignalRConnectionId, jobId);

            // Invoke method on specific runner via SignalR
            await _hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                RunnerEndpoints.Plan,
                new PlanRequestBase
                {
                    JobId = jobId,
                    OrganizationId = orgId,
                    Metadata = metadata,
                    Engine = msg.Declared.Engine,
                    PlanBeforeHook = msg.Declared.PlanBeforeHook,
                    PlanAfterHook = msg.Declared.PlanAfterHook,
                    // Pre-resolved Terraform variables from server
                    ResolvedParameters = resolvedParameters,
                    PulumiFlags = msg.Declared.PulumiFlags
                        .Where(f => f.Task == PulumiCommandTask.Plan)
                        .ToList(),
                    PulumiArrayFlags = msg.Declared.PulumiArrayFlags
                        .Where(f => f.Task == PulumiCommandTask.Plan)
                        .ToList(),
                    TerraformFlags = msg.Declared.TerraformFlags
                        .Where(f => f.Task == TerraformCommandTask.Plan)
                        .ToList(),
                    TerraformArrayFlags = msg.Declared.TerraformArrayFlags
                        .Where(f => f.Task == TerraformCommandTask.Plan)
                        .ToList(),
                    Policies = PolicyApplicability.ForPlanStep(msg.Declared, msg.IsDestroyJob)
                }
            );

            _logger.LogDebug("Dispatched Plan request to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching Plan request for job {JobId}", jobId);
            await context.Publish(new PlanFaulted
            {
                CorrelationId = jobId,
                OrganizationId = orgId,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                IsServerSideError = true
            });
        }
    }
}