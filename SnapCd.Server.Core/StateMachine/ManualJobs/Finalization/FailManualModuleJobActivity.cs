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
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.ManualJobs.Finalization;

/// <summary>
/// Closes a failed manual job. Parallel to FailModuleJobActivity rather than shared: that one
/// writes ModuleJobs and sets an ActualStateHeadline, which is deployment vocabulary a split has
/// no equivalent for.
/// </summary>
public class FailManualModuleJobActivity<TSaga, TMessage> : IStateMachineActivity<TSaga, TMessage>
    where TSaga : ManualJobSagaBase
    where TMessage : class
{
    private readonly ManualModuleJobRepository _repository;

    public FailManualModuleJobActivity(ManualModuleJobRepository repository)
    {
        _repository = repository;
    }

    public async Task Execute(
        BehaviorContext<TSaga, TMessage> context,
        IBehavior<TSaga, TMessage> next)
    {
        if (context.Message is StepFaultedBase { IsServerSideError: true } faulted)
        {
            var fullMessage = !string.IsNullOrEmpty(faulted.StackTrace)
                ? $"{faulted.ErrorMessage}\n\nStack Trace:\n{faulted.StackTrace}"
                : faulted.ErrorMessage;

            var failedStep = StepMapper.DetermineStepFromEventType(typeof(TMessage));

            await _repository.FinalizeWithServerError(
                context.Saga.CorrelationId,
                context.Saga.OrganizationId,
                DateTimeOffset.UtcNow,
                failedStep,
                $"Server error during {failedStep}",
                fullMessage);
        }
        else
        {
            await _repository.Finalize(
                context.Saga.CorrelationId,
                context.Saga.OrganizationId,
                ExecutionStatus.Failed,
                DateTimeOffset.UtcNow);
        }

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TMessage, TException> context,
        IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context) => context.CreateScope("fail-manual-module-job");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
