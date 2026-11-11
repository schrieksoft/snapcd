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

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites.Finalization;

/// <summary>
/// Ends a job cancelled while it was waiting for an approval. Separate from CancelModuleJobActivity,
/// which serves the arms outside that state and must not record an approval outcome.
/// </summary>
public class CancelWhileWaitingForApprovalModuleJobActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : JobSagaBase
    where TMessage : class
{
    private readonly ModuleJobRepository _repository;

    public CancelWhileWaitingForApprovalModuleJobActivity(ModuleJobRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(
        BehaviorContext<TSaga, TMessage> context,
        IBehavior<TSaga, TMessage> next)
    {
        var actualStateHeadline = typeof(TSaga).Name switch
        {
            "ApplyJobSaga" => ActualStateHeadline.ApplyCancelled,
            "DestroyJobSaga" => ActualStateHeadline.DestroyCancelled,
            _ => (ActualStateHeadline?)null
        };

        await _repository.RecordApprovalDecision(
            context.Saga.CorrelationId,
            context.Saga.OrganizationId,
            ApprovalDecision.Cancelled);

        await _repository.Finalize(
            context.Saga.CorrelationId,
            context.Saga.OrganizationId,
            ExecutionStatus.Cancelled,
            typeof(TMessage).Name,
            DateTimeOffset.UtcNow,
            null,
            actualStateHeadline);

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TMessage, TException> context,
        IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("cancel-while-waiting-for-approval-module-job");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
