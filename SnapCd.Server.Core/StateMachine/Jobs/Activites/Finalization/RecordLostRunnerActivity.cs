// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites.Finalization;

/// <summary>
/// Records a lost runner on the job without finalizing it, for a path that ends the job some other way.
/// </summary>
public class RecordLostRunnerActivity<TSaga> : IStateMachineActivity<TSaga, HeartbeatFailed>
    where TSaga : JobSagaBase
{
    private readonly ModuleJobRepository _repository;

    public RecordLostRunnerActivity(ModuleJobRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(
        BehaviorContext<TSaga, HeartbeatFailed> context,
        IBehavior<TSaga, HeartbeatFailed> next)
    {
        await _repository.SetServerSideError(
            context.Saga.CorrelationId,
            context.Saga.OrganizationId,
            StepMapper.DetermineStepFromState(context.Saga.CurrentState),
            LostRunnerError.Header,
            LostRunnerError.Message(context.Saga, context.Message.Reason));

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
        context.CreateScope("record-lost-runner");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
