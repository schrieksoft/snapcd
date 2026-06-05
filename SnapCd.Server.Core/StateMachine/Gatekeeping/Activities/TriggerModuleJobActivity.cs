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

namespace SnapCd.Server.Core.StateMachine.Gatekeeping.Activities;

public class TriggerModuleJobActivity<TGatekeepingJobRequested> :
    IStateMachineActivity<ModuleSaga, TGatekeepingJobRequested>
    where TGatekeepingJobRequested : GatekeepingJobRequestedBase
{
    private readonly JobService _executionService;
    private readonly ILogger<TriggerModuleJobActivity<TGatekeepingJobRequested>> _logger;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public TriggerModuleJobActivity(JobService executionService, ILogger<TriggerModuleJobActivity<TGatekeepingJobRequested>> logger, IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _executionService = executionService;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    public async Task Execute(
        BehaviorContext<ModuleSaga, TGatekeepingJobRequested> context,
        IBehavior<ModuleSaga, TGatekeepingJobRequested> next)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            // Determine effective desired state early
            var effectiveDesiredState = context.Message.SetNewDesiredState
                ? context.Message.DesiredStateHeadline
                : context.Saga.DesiredStateHeadline;

            // Skip if module is already destroyed and desired state is destroyed
            // (Unlike Apply, Destroy is idempotent - a destroyed module never needs to be destroyed again)
            if (effectiveDesiredState == DesiredStateHeadline.Destroyed)
            {
                var actualStateHeadline = await dbContext.ModuleJobs
                    .Where(j => j.ModuleId == context.Saga.CorrelationId &&
                                j.OrganizationId == context.Saga.OrganizationId &&
                                j.ActualStateHeadline != null &&
                                j.TimestampEnd != null)
                    .OrderByDescending(j => j.TimestampEnd)
                    .Select(j => j.ActualStateHeadline)
                    .FirstOrDefaultAsync();

                if (actualStateHeadline == ActualStateHeadline.Destroyed)
                {
                    _logger.LogInformation(
                        "Module {ModuleId} already destroyed, skipping",
                        context.Saga.CorrelationId);
                    await next.Execute(context).ConfigureAwait(false);
                    return;
                }
            }

            // Check if there's already a current ModuleJob for this module
            var currentJob = await dbContext.ModuleJobs
                .Where(j => j.ModuleId == context.Message.ModuleId && j.OrganizationId == context.Message.OrganizationId && j.IsCurrent == true)
                .FirstOrDefaultAsync();

            var canExecute = await _executionService.CheckDependenciesAsync(
                context.Saga.CorrelationId,
                context.Message.OrganizationId,
                context.Message.DesiredStateHeadline);

            if (!canExecute)
            {
                // effectiveDesiredState already calculated at the top of the method
                if (context.Message.SetNewDesiredState || context.Saga.QueuedDesiredStateHeadline == null)
                {
                    context.Saga.QueuedDesiredStateHeadline = effectiveDesiredState;
                    context.Saga.QueuedReason = QueuedReason.WaitingOnDependencies;
                }

                _logger.LogInformation(
                    "Module {ModuleId} waiting on dependencies, queuing request",
                    context.Message.ModuleId);

                // Proceed to the next activity
                await next.Execute(context).ConfigureAwait(false);
            }
            else if (currentJob == null && !context.Saga.QueuedDesiredStateHeadline.HasValue)
            {
                if (context.Message.SetNewDesiredState)
                    context.Saga.DesiredStateHeadline = context.Message.DesiredStateHeadline;

                if (context.Message.DesiredStateHeadline == context.Saga.DesiredStateHeadline)
                {
                    var hasActiveRunner = await _executionService.CheckRunnerAvailabilityAsync(context.Message.ModuleId);
                    if (!hasActiveRunner)
                    {
                        if (context.Message.SetNewDesiredState || context.Saga.QueuedDesiredStateHeadline == null)
                        {
                            // effectiveDesiredState already calculated at the top of the method
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
                            await _executionService.Apply(context.Message.ModuleId, context.Message.OrganizationId, context.Message.JobId, context.Message.RunnerInstanceNameOverride);
                            _logger.LogInformation(
                                "Running Apply for module {ModuleId} (no current job)",
                                context.Message.ModuleId);
                            break;
                        case DesiredStateHeadline.Destroyed:
                            await _executionService.Destroy(context.Message.ModuleId, context.Message.OrganizationId, context.Message.JobId, context.Message.RunnerInstanceNameOverride);
                            _logger.LogInformation(
                                "Running Destroy for module {ModuleId} (no current job)",
                                context.Message.ModuleId);
                            break;
                    }

                    await next.Execute(context).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogWarning(
                        "Skipping: message requested state {RequestedState} but saga is currently set to state {SagaState}",
                        context.Message.DesiredStateHeadline, context.Saga.DesiredStateHeadline);
                    await next.Execute(context).ConfigureAwait(false);
                }
            }
            else if (currentJob == null && context.Saga.QueuedDesiredStateHeadline.HasValue) // No currently job but still queued, so waiting on depedencies
            {
                if (canExecute)
                {
                    var hasActiveRunner = await _executionService.CheckRunnerAvailabilityAsync(context.Saga.CorrelationId);
                    if (!hasActiveRunner)
                    {
                        _logger.LogInformation(
                            "No active runners available for module {ModuleId}, keeping queued",
                            context.Saga.CorrelationId);
                    }
                    else
                    {
                        // Dependencies are met and runner is available, dequeue and execute
                        context.Saga.DesiredStateHeadline = context.Saga.QueuedDesiredStateHeadline.Value;
                        context.Saga.QueuedDesiredStateHeadline = null;
                        context.Saga.QueuedReason = null;

                        if (context.Saga.DesiredStateHeadline == DesiredStateHeadline.Applied)
                        {
                            await _executionService.Apply(context.Saga.CorrelationId, context.Saga.OrganizationId);
                            _logger.LogInformation(
                                "Dependencies met and runner available, dequeued and running Apply for module {ModuleId}",
                                context.Saga.CorrelationId);
                        }
                        else if (context.Saga.DesiredStateHeadline == DesiredStateHeadline.Destroyed)
                        {
                            await _executionService.Destroy(context.Saga.CorrelationId, context.Saga.OrganizationId);
                            _logger.LogInformation(
                                "Dependencies met and runner available, dequeued and running Destroy for module {ModuleId}",
                                context.Saga.CorrelationId);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "Dependencies not yet met for module {ModuleId}, keeping queued",
                        context.Saga.CorrelationId);
                }
            }
            else
            {
                context.Saga.QueuedDesiredStateHeadline = context.Message is { SetNewDesiredState: true }
                    ? context.Message.DesiredStateHeadline
                    : context.Saga.DesiredStateHeadline;
                context.Saga.QueuedReason = QueuedReason.WaitingOnRunningJob;

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