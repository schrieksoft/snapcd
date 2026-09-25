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
/// Marks a job as waiting for the other module to agree. The wait has no timeout, so a job that
/// does not say it is waiting is indistinguishable from a hung one.
/// </summary>
public class WaitingForConsentActivity<TSaga, TMessage>(ManualModuleJobRepository jobs)
    : IStateMachineActivity<TSaga, TMessage>
    where TSaga : ManualJobSagaBase
    where TMessage : class
{
    public async Task Execute(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        await jobs.WaitingForConsent(context.Saga.CorrelationId, context.Saga.OrganizationId, true);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
        => next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("waiting-for-consent");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}

/// <summary>Clears the mark once the other module has answered, either way.</summary>
public class NotWaitingForConsentActivity<TSaga, TMessage>(ManualModuleJobRepository jobs)
    : IStateMachineActivity<TSaga, TMessage>
    where TSaga : ManualJobSagaBase
    where TMessage : class
{
    public async Task Execute(BehaviorContext<TSaga, TMessage> context, IBehavior<TSaga, TMessage> next)
    {
        await jobs.WaitingForConsent(context.Saga.CorrelationId, context.Saga.OrganizationId, false);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TMessage, TException> context, IBehavior<TSaga, TMessage> next)
        where TException : Exception
        => next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("not-waiting-for-consent");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
