using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.StateMachine.Gatekeeping.Activities;

public class MaybeEmitModuleStateChangedToAppliedEvent<TMessage> :
    IStateMachineActivity<ModuleSaga, TMessage> where TMessage : class
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly ILogger<MaybeEmitModuleStateChangedToAppliedEvent<TMessage>> _logger;
    private readonly ModuleJobRepositoryFactory _repositoryFactory;

    public MaybeEmitModuleStateChangedToAppliedEvent(
        ModuleJobRepositoryFactory repositoryFactory,
        ILogger<MaybeEmitModuleStateChangedToAppliedEvent<TMessage>> logger)
    {
        _repositoryFactory = repositoryFactory;
        _logger = logger;
    }

    public async Task Execute(
        BehaviorContext<ModuleSaga, TMessage> context,
        IBehavior<ModuleSaga, TMessage> next)
    {
        try
        {
            using var moduleJobRepo = _repositoryFactory.Create();

            var recentStates = await moduleJobRepo.GetRecentActualDefiniteRevisions(context.Saga.CorrelationId, 2);

            var shouldEmit = false;

            if (recentStates.Count == 1)
            {
                // First ever job - emit if it's Applied
                shouldEmit = recentStates[0] == ActualStateHeadline.Applied;
                _logger.LogInformation("Module {ModuleId}: First job completed with state {State}, emitting: {ShouldEmit}",
                    context.Saga.CorrelationId, recentStates[0], shouldEmit);
            }
            else if (recentStates.Count >= 2)
            {
                // Check for state transition to Applied
                var currentState = recentStates[0];
                var previousState = recentStates[1];
                shouldEmit = currentState == ActualStateHeadline.Applied && previousState != ActualStateHeadline.Applied;

                _logger.LogInformation("Module {ModuleId}: State transition {PreviousState} -> {CurrentState}, emitting: {ShouldEmit}",
                    context.Saga.CorrelationId, previousState, currentState, shouldEmit);
            }
            else
            {
                _logger.LogInformation("Module {ModuleId}: No completed jobs found, not emitting", context.Saga.CorrelationId);
            }

            if (shouldEmit)
            {
                await context.Publish(new ModuleStateChangedToAppliedEvent { ModuleId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId });
                _logger.LogInformation("Module {ModuleId}: Emitted ModuleStateChangedToAppliedEvent", context.Saga.CorrelationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MaybeEmitModuleStateChangedToAppliedEvent for module {ModuleId}", context.Saga.CorrelationId);
        }

        // Always proceed to next activity
        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<ModuleSaga, TMessage, TException> context,
        IBehavior<ModuleSaga, TMessage> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("maybe-emit-module-state-changed-to-applied");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}