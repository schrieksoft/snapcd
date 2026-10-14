// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.StateMachine.RippleProbes.Finalization;

/// <summary>Closes a ripple probe that reported its signals.</summary>
public class CompleteRippleProbeJobActivity<TMessage> : IStateMachineActivity<RippleProbeSaga, TMessage>
    where TMessage : class
{
    private readonly RippleProbeJobRepository _repository;

    public CompleteRippleProbeJobActivity(RippleProbeJobRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(
        BehaviorContext<RippleProbeSaga, TMessage> context,
        IBehavior<RippleProbeSaga, TMessage> next)
    {
        await _repository.ExecuteFinalize(
            context.Saga.CorrelationId,
            context.Saga.OrganizationId,
            ExecutionStatus.Completed,
            DateTimeOffset.UtcNow);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<RippleProbeSaga, TMessage, TException> context,
        IBehavior<RippleProbeSaga, TMessage> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context) => context.CreateScope("complete-ripple-probe-job");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
