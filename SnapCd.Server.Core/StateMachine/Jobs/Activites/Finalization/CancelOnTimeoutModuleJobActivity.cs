// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

//using MassTransit;

using MassTransit;
using MassTransit.Contracts;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites.Finalization;

public class CancelOnTimeoutModuleJobActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, RequestTimeoutExpired<TMessage>>
    where TSaga : JobSagaBase, SagaStateMachineInstance
    where TMessage : class
{
    private readonly ModuleJobRepository _repository;

    public CancelOnTimeoutModuleJobActivity(ModuleJobRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(
        BehaviorContext<TSaga, RequestTimeoutExpired<TMessage>> context,
        IBehavior<TSaga, RequestTimeoutExpired<TMessage>> next)
    {
        // Determine ActualStateHeadline based on job type for cancelled jobs
        var actualStateHeadline = typeof(TSaga).Name switch
        {
            "ApplyJobSaga" => ActualStateHeadline.ApplyCancelled,
            "DestroyJobSaga" => ActualStateHeadline.DestroyCancelled,
            _ => (ActualStateHeadline?)null
        };
        
        var runner = string.IsNullOrEmpty(context.Saga.RunnerInstanceName)
            ? context.Saga.RunnerName
            : $"{context.Saga.RunnerName} ({context.Saga.RunnerInstanceName})";

        await _repository.SetServerSideError(
            context.Saga.CorrelationId,
            context.Saga.OrganizationId,
            null,
            "Runner did not confirm the cancel",
            $"The server asked runner '{runner}' to stop the job, but the runner did not confirm within the cancel timeout, so the job was closed as cancelled. "
            + "If the runner was still connected, the step it was running may have continued. Check the runner's logs, and the server's log for why the request could not be delivered.");

        await _repository.Finalize(
            context.Saga.CorrelationId,
            context.Saga.OrganizationId,
            ExecutionStatus.Cancelled,
            typeof(TMessage).Name,
            DateTimeOffset.UtcNow,
            null,
            actualStateHeadline);


        // Proceed to the next activity
        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, RequestTimeoutExpired<TMessage>, TException> context,
        IBehavior<TSaga, RequestTimeoutExpired<TMessage>> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("cancel-on-timeout-module-job");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}