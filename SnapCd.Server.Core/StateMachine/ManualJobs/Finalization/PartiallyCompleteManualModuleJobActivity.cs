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
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.StateMachine.ManualJobs.Finalization;

/// <summary>
/// Ends a job that did some of what it was asked. The work that succeeded stands, so this is not a
/// failure: the addresses that did not are recorded and can be run again.
/// </summary>
public class PartiallyCompleteManualModuleJobActivity<TSaga, TMessage> : IStateMachineActivity<TSaga, TMessage>
    where TSaga : ManualJobSagaBase
    where TMessage : class
{
    private readonly ManualModuleJobRepository _repository;

    public PartiallyCompleteManualModuleJobActivity(ManualModuleJobRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(
        BehaviorContext<TSaga, TMessage> context,
        IBehavior<TSaga, TMessage> next)
    {
        await _repository.Finalize(
            context.Saga.CorrelationId,
            context.Saga.OrganizationId,
            ExecutionStatus.PartiallyCompleted,
            DateTimeOffset.UtcNow);

        await context.Publish(new ManualJobUpdatedEvent
        {
            JobId = context.Saga.CorrelationId,
            ModuleId = context.Saga.ModuleId,
            OrganizationId = context.Saga.OrganizationId
        });

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TMessage, TException> context,
        IBehavior<TSaga, TMessage> next)
        where TException : Exception
        => next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("partially-complete-manual-job");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
