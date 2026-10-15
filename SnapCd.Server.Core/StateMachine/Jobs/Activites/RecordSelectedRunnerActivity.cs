// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites;

/// <summary>
/// Copies the saga's selected runner onto the job row. The saga is deleted when the job finishes,
/// so the row is the only place the fact survives, and it is what authorizes a runner writing
/// against a job it was not given.
///
/// One per job table rather than one generic over the three, because each table has its own
/// repository with its own update convention, and an activity is resolved from DI by its closed
/// generic type.
/// </summary>
public class RecordSelectedRunnerActivity<TSaga, TMessage> : IStateMachineActivity<TSaga, TMessage>
    where TSaga : JobSagaBase
    where TMessage : class
{
    private readonly ModuleJobRepository _jobs;

    public RecordSelectedRunnerActivity(ModuleJobRepository jobs) => _jobs = jobs;

    public async Task Execute(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        await _jobs.RecordSelectedRunner(
            context.Saga.CorrelationId, context.Saga.OrganizationId, context.Saga.RunnerId);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
        => next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("record-selected-runner");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}

/// <summary>The same for the manual job tables. See <see cref="RecordSelectedRunnerActivity{TSaga,TMessage}"/>.</summary>
public class RecordSelectedStateMigrationRunnerActivity<TSaga, TMessage> : IStateMachineActivity<TSaga, TMessage>
    where TSaga : StateMigrationSagaBase
    where TMessage : class
{
    private readonly StateMigrationJobRepository _jobs;

    public RecordSelectedStateMigrationRunnerActivity(StateMigrationJobRepository jobs) => _jobs = jobs;

    public async Task Execute(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        await _jobs.ExecuteRecordSelectedRunner(
            context.Saga.CorrelationId, context.Saga.OrganizationId, context.Saga.RunnerId);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
        => next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("record-selected-runner");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
