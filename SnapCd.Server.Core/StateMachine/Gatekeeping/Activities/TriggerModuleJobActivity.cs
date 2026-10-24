// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.Services.ModuleHold;

namespace SnapCd.Server.Core.StateMachine.Gatekeeping.Activities;

public class TriggerModuleJobActivity<TGatekeepingJobRequested> :
    IStateMachineActivity<ModuleSaga, TGatekeepingJobRequested>
    where TGatekeepingJobRequested : GatekeepingJobRequestedBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMaintenanceModeService _maintenanceMode;
    private readonly ILogger<TriggerModuleJobActivity<TGatekeepingJobRequested>> _logger;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public TriggerModuleJobActivity(IServiceProvider serviceProvider, IMaintenanceModeService maintenanceMode, ILogger<TriggerModuleJobActivity<TGatekeepingJobRequested>> logger, IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _serviceProvider = serviceProvider;
        _maintenanceMode = maintenanceMode;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    // Resolved lazily: the maintenance-gated path runs without the job-service dependency graph.
    private JobService ExecutionService => _serviceProvider.GetRequiredService<JobService>();
    private ModuleHoldGate HoldGate => _serviceProvider.GetRequiredService<ModuleHoldGate>();

    public async Task Execute(
        BehaviorContext<ModuleSaga, TGatekeepingJobRequested> context,
        IBehavior<ModuleSaga, TGatekeepingJobRequested> next)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var explicitRequest = context.Message.SetNewDesiredState;
            if (explicitRequest && context.Message.DesiredStateHeadline == null)
                throw new InvalidOperationException("A request that sets a new desired state must say which state.");

            // What the module is headed for: a queued request, else the state of its last job.
            var target = context.Saga.QueuedDesiredStateHeadline ?? context.Saga.DesiredStateHeadline;

            if (!explicitRequest && target == null)
            {
                _logger.LogDebug("Module {ModuleId} has never been asked for a state: nothing to do", context.Message.ModuleId);
                await next.Execute(context).ConfigureAwait(false);
                return;
            }

            // The state this request runs or queues.
            var effectiveDesiredState = explicitRequest ? context.Message.DesiredStateHeadline!.Value : target!.Value;

            // A request naming a state without setting it only acts while that state is still the target.
            if (!explicitRequest && context.Message.DesiredStateHeadline is { } namedState && namedState != target)
            {
                _logger.LogDebug(
                    "Module {ModuleId} is headed for {Target}, not {Requested}: nothing to do",
                    context.Message.ModuleId, target, namedState);
                await next.Execute(context).ConfigureAwait(false);
                return;
            }

            _logger.LogDebug(
                "Gate entered for module {ModuleId} job {JobId}: message wants {MessageState} "
                + "(SetNewDesiredState={SetNew}), saga is {SagaState}, effective {EffectiveState}, "
                + "queued {QueuedState}, paused {Paused}",
                context.Message.ModuleId, context.Message.JobId, context.Message.DesiredStateHeadline,
                context.Message.SetNewDesiredState, context.Saga.DesiredStateHeadline,
                effectiveDesiredState, context.Saga.QueuedDesiredStateHeadline, context.Saga.Paused);

            if (await _maintenanceMode.IsActiveAsync())
            {
                if (MayQueue(explicitRequest, context.Saga, effectiveDesiredState))
                {
                    context.Saga.QueuedDesiredStateHeadline = effectiveDesiredState;
                    context.Saga.QueuedReason = QueuedReason.Maintenance;
                }

                _logger.LogInformation(
                    "Maintenance mode active: queuing request for module {ModuleId}",
                    context.Message.ModuleId);
                await next.Execute(context).ConfigureAwait(false);
                return;
            }

            if (context.Saga.Paused)
            {
                if (MayQueue(explicitRequest, context.Saga, effectiveDesiredState))
                {
                    context.Saga.QueuedDesiredStateHeadline = effectiveDesiredState;
                    context.Saga.QueuedReason = QueuedReason.Paused;
                }

                _logger.LogDebug("Module {ModuleId} is paused: queuing request", context.Message.ModuleId);
                await next.Execute(context).ConfigureAwait(false);
                return;
            }

            var stateToQueue = MayQueue(explicitRequest, context.Saga, effectiveDesiredState) ? effectiveDesiredState : (DesiredStateHeadline?)null;
            if (await HoldGate.ParkIfHeld(context.Saga, stateToQueue, replaceQueue: true))
            {
                await next.Execute(context).ConfigureAwait(false);
                return;
            }

            if (effectiveDesiredState == DesiredStateHeadline.Destroyed &&
                await ModuleDestroyedCheck.IsDestroyedAsync(dbContext, context.Saga.CorrelationId, context.Saga.OrganizationId))
            {
                if (context.Saga.QueuedDesiredStateHeadline == DesiredStateHeadline.Destroyed)
                {
                    context.Saga.QueuedDesiredStateHeadline = null;
                    context.Saga.QueuedReason = null;
                }

                _logger.LogInformation(
                    "Module {ModuleId} already destroyed, skipping",
                    context.Saga.CorrelationId);
                await next.Execute(context).ConfigureAwait(false);
                return;
            }

            // Check if there's already a current ModuleJob for this module
            var currentJob = await dbContext.ModuleJobs
                .Where(j => j.ModuleId == context.Message.ModuleId && j.OrganizationId == context.Message.OrganizationId && j.IsCurrent == true)
                .FirstOrDefaultAsync();

            var canExecute = await ExecutionService.CheckDependenciesAsync(
                context.Saga.CorrelationId,
                context.Message.OrganizationId,
                effectiveDesiredState);

            _logger.LogDebug(
                "Gate for module {ModuleId}: currentJob {CurrentJob}, canExecute {CanExecute}",
                context.Message.ModuleId, currentJob?.Id, canExecute);

            if (!canExecute)
            {
                if (MayQueue(explicitRequest, context.Saga, effectiveDesiredState))
                {
                    context.Saga.QueuedDesiredStateHeadline = effectiveDesiredState;
                    context.Saga.QueuedReason = QueuedReason.WaitingOnDependencies;
                }

                _logger.LogInformation(
                    "Module {ModuleId} waiting on dependencies to {State}, queuing request",
                    context.Message.ModuleId, effectiveDesiredState);

                // Proceed to the next activity
                await next.Execute(context).ConfigureAwait(false);
            }
            else if (currentJob == null && !context.Saga.QueuedDesiredStateHeadline.HasValue)
            {
                if (explicitRequest)
                    context.Saga.DesiredStateHeadline = effectiveDesiredState;

                // A destroy is only ever started by asking for one.
                if (explicitRequest || effectiveDesiredState != DesiredStateHeadline.Destroyed)
                {
                    var hasActiveRunner = await ExecutionService.CheckRunnerAvailabilityAsync(context.Message.ModuleId);
                    if (!hasActiveRunner)
                    {
                        if (MayQueue(explicitRequest, context.Saga, effectiveDesiredState))
                        {
                            context.Saga.QueuedDesiredStateHeadline = effectiveDesiredState;
                            context.Saga.QueuedReason = QueuedReason.WaitingOnRunnerCheckin;
                        }

                        _logger.LogInformation(
                            "No active runners available for module {ModuleId}, queuing request",
                            context.Message.ModuleId);
                        await next.Execute(context).ConfigureAwait(false);
                        return;
                    }

                    switch (context.Saga.DesiredStateHeadline)
                    {
                        // No current job, trigger a new one
                        case DesiredStateHeadline.Applied:
                            await ExecutionService.Apply(context.Message.ModuleId, context.Message.OrganizationId, context.Message.JobId, context.Message.RunnerInstanceNameOverride);
                            _logger.LogInformation(
                                "Running Apply for module {ModuleId} (no current job)",
                                context.Message.ModuleId);
                            break;
                        case DesiredStateHeadline.Destroyed:
                            await ExecutionService.Destroy(context.Message.ModuleId, context.Message.OrganizationId, context.Message.JobId, context.Message.RunnerInstanceNameOverride);
                            _logger.LogInformation(
                                "Running Destroy for module {ModuleId} (no current job)",
                                context.Message.ModuleId);
                            break;
                        default:
                            // Nothing is dispatched and the job waits forever, so say so.
                            _logger.LogWarning(
                                "Gate dispatched nothing for module {ModuleId}: desired state is "
                                + "{SagaState}, which is neither Applied nor Destroyed",
                                context.Message.ModuleId, context.Saga.DesiredStateHeadline);
                            break;
                    }

                    await next.Execute(context).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogDebug(
                        "Module {ModuleId} is headed for Destroyed and nothing asked for a destroy: nothing to do",
                        context.Message.ModuleId);
                    await next.Execute(context).ConfigureAwait(false);
                }
            }
            else if (currentJob == null && context.Saga.QueuedDesiredStateHeadline.HasValue) // No current job but still queued, so waiting on dependencies
            {
                if (canExecute)
                {
                    var hasActiveRunner = await ExecutionService.CheckRunnerAvailabilityAsync(context.Saga.CorrelationId);
                    if (!hasActiveRunner)
                    {
                        if (explicitRequest)
                        {
                            context.Saga.QueuedDesiredStateHeadline = effectiveDesiredState;
                            context.Saga.QueuedReason = QueuedReason.WaitingOnRunnerCheckin;
                        }

                        _logger.LogInformation(
                            "No active runners available for module {ModuleId}, keeping queued",
                            context.Saga.CorrelationId);
                    }
                    else
                    {
                        // Dequeue and execute. An explicit request (SetNewDesiredState) OVERRIDES
                        // whatever is queued
                        context.Saga.DesiredStateHeadline = effectiveDesiredState;
                        context.Saga.QueuedDesiredStateHeadline = null;
                        context.Saga.QueuedReason = null;

                        if (context.Saga.DesiredStateHeadline == DesiredStateHeadline.Applied)
                        {
                            await ExecutionService.Apply(context.Saga.CorrelationId, context.Saga.OrganizationId, context.Message.JobId, context.Message.RunnerInstanceNameOverride);
                            _logger.LogInformation(
                                "Dependencies met and runner available, dequeued and running Apply for module {ModuleId}",
                                context.Saga.CorrelationId);
                        }
                        else if (context.Saga.DesiredStateHeadline == DesiredStateHeadline.Destroyed)
                        {
                            await ExecutionService.Destroy(context.Saga.CorrelationId, context.Saga.OrganizationId, context.Message.JobId, context.Message.RunnerInstanceNameOverride);
                            _logger.LogInformation(
                                "Dependencies met and runner available, dequeued and running Destroy for module {ModuleId}",
                                context.Saga.CorrelationId);
                        }
                    }
                }
                else
                {
                    // Dependencies still unmet — but an explicit request still replaces what is
                    // queued, so it runs once they are met rather than the stale opposite.
                    if (explicitRequest)
                    {
                        context.Saga.QueuedDesiredStateHeadline = effectiveDesiredState;
                        context.Saga.QueuedReason = QueuedReason.WaitingOnDependencies;
                    }

                    _logger.LogInformation(
                        "Dependencies not yet met for module {ModuleId} to {State}, keeping queued",
                        context.Saga.CorrelationId, effectiveDesiredState);
                }

                await next.Execute(context).ConfigureAwait(false);
            }
            else
            {
                if (MayQueue(explicitRequest, context.Saga, effectiveDesiredState))
                {
                    context.Saga.QueuedDesiredStateHeadline = effectiveDesiredState;
                    context.Saga.QueuedReason = QueuedReason.WaitingOnRunningJob;
                }

                _logger.LogInformation(
                    "Job already running for module {ModuleId}, queuing request",
                    context.Message.ModuleId);

                // Proceed to the next activity
                await next.Execute(context).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error executing TriggerModuleJobActivity for module {ModuleId}",
                context.Message.ModuleId);
        }
    }

    /// <summary>
    /// An explicit request always replaces the queue. Any other request may only fill an empty queue, and never
    /// with a destroy: re-running one achieves nothing.
    /// </summary>
    private static bool MayQueue(bool explicitRequest, ModuleSaga saga, DesiredStateHeadline state) =>
        explicitRequest || (saga.QueuedDesiredStateHeadline == null && state != DesiredStateHeadline.Destroyed);

    public Task Faulted<TException>(
        BehaviorExceptionContext<ModuleSaga, TGatekeepingJobRequested, TException> context,
        IBehavior<ModuleSaga, TGatekeepingJobRequested> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("trigger-module-job");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}