// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites.Finalization;

/// <summary>
/// Fails the job with a server-side error when a step's message could not be handled: a dispatch consumer threw,
/// or this state machine's own handling of a step's result threw. MassTransit raises the fault after the endpoint's retries.
/// </summary>
public class FailModuleJobOnFaultActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, Fault<TMessage>>
    where TSaga : JobSagaBase
    where TMessage : class
{
    private readonly ModuleJobRepository _repository;

    public FailModuleJobOnFaultActivity(ModuleJobRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(
        BehaviorContext<TSaga, Fault<TMessage>> context,
        IBehavior<TSaga, Fault<TMessage>> next)
    {
        var actualStateHeadline = typeof(TSaga).Name switch
        {
            "ApplyJobSaga" => ActualStateHeadline.ApplyFailed,
            "DestroyJobSaga" => ActualStateHeadline.DestroyFailed,
            _ => (ActualStateHeadline?)null
        };

        var failedStep = StepMapper.DetermineStepFromState(context.Saga.CurrentState);

        await _repository.FinalizeWithServerError(
            context.Saga.CorrelationId,
            context.Saga.OrganizationId,
            $"Fault<{typeof(TMessage).Name}>",
            DateTimeOffset.UtcNow,
            actualStateHeadline,
            failedStep,
            $"Server error while handling {typeof(TMessage).Name}",
            FaultText.Describe(context.Message.Exceptions));

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, Fault<TMessage>, TException> context,
        IBehavior<TSaga, Fault<TMessage>> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("fail-module-job-on-fault");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
