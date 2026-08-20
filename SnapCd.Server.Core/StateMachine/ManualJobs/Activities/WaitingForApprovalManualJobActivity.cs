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

namespace SnapCd.Server.Core.StateMachine.ManualJobs.Activities;

/// <summary>
/// Writes the job row's waiting-for-approval flag, which is what the dashboard reads to decide
/// whether to offer the decision. The saga's own state is not visible to it.
/// </summary>
public class WaitingForApprovalManualJobActivity<TSaga, TMessage> : IStateMachineActivity<TSaga, TMessage>
    where TSaga : ManualJobSagaBase
    where TMessage : class
{
    private readonly ManualModuleJobRepository _repository;

    public WaitingForApprovalManualJobActivity(ManualModuleJobRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(
        BehaviorContext<TSaga, TMessage> context,
        IBehavior<TSaga, TMessage> next)
    {
        await _repository.WaitingForApproval(context.Saga.CorrelationId, context.Saga.OrganizationId, true);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TMessage, TException> context,
        IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context) => context.CreateScope("waiting-for-approval-manual-job");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
