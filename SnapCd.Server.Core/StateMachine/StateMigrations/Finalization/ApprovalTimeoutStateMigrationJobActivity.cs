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

namespace SnapCd.Server.Core.StateMachine.StateMigrations.Finalization;

/// <summary>
/// Ends a state migration whose approval was not answered in time. Separate from CancelStateMigrationJobActivity, which serves every
/// other way a state migration ends and must not record an approval outcome.
/// </summary>
public class ApprovalTimeoutStateMigrationJobActivity<TSaga, TMessage> : IStateMachineActivity<TSaga, TMessage>
    where TSaga : StateMigrationSagaBase
    where TMessage : class
{
    private readonly StateMigrationJobRepository _repository;

    public ApprovalTimeoutStateMigrationJobActivity(StateMigrationJobRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(
        BehaviorContext<TSaga, TMessage> context,
        IBehavior<TSaga, TMessage> next)
    {
        await _repository.ExecuteRecordApprovalDecision(
            context.Saga.CorrelationId,
            context.Saga.OrganizationId,
            ApprovalDecision.TimedOut);

        await _repository.ExecuteFinalize(
            context.Saga.CorrelationId,
            context.Saga.OrganizationId,
            ExecutionStatus.Cancelled,
            DateTimeOffset.UtcNow);

        await context.Publish(new StateMigrationUpdatedEvent
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
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context) => context.CreateScope("approval-timeout-state-migration-job");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
