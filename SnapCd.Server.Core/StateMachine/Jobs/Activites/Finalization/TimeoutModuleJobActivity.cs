// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

//using MassTransit;

using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites.Finalization;

public class TimeoutModuleJobActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, HeartbeatFailed>
    where TSaga : JobSagaBase, SagaStateMachineInstance
    where TMessage : class
{
    private readonly ModuleJobRepository _repository;

    public TimeoutModuleJobActivity(ModuleJobRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(
        BehaviorContext<TSaga, HeartbeatFailed> context,
        IBehavior<TSaga, HeartbeatFailed> next)
    {
        var actualStateHeadline = typeof(TSaga).Name switch
        {
            "ApplyJobSaga" => ActualStateHeadline.ApplyTimeout,
            "DestroyJobSaga" => ActualStateHeadline.DestroyTimeout,
            _ => (ActualStateHeadline?)null
        };

        var runner = string.IsNullOrEmpty(context.Saga.RunnerInstanceName)
            ? context.Saga.RunnerName
            : $"{context.Saga.RunnerName} ({context.Saga.RunnerInstanceName})";

        var errorMessage =
            $"The job was assigned to runner '{runner}' but stopped sending heartbeats, so the server ended it. "
            + $"The job was in state '{context.Saga.CurrentState}' at the time. "
            + "The runner may have lost its connection to the server, been stopped, or failed before it could report an error. "
            + "Check the runner's logs for the period leading up to this job's end time.";

        await _repository.FinalizeWithServerError(
            context.Saga.CorrelationId,
            context.Saga.OrganizationId,
            typeof(TMessage).Name,
            DateTimeOffset.UtcNow,
            actualStateHeadline,
            StepMapper.DetermineStepFromEventType(typeof(TMessage)),
            "Runner stopped responding",
            errorMessage);


        // Proceed to the next activity
        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, HeartbeatFailed, TException> context,
        IBehavior<TSaga, HeartbeatFailed> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("timeout-module-job");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}