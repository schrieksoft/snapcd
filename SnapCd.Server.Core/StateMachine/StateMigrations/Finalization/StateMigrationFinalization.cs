// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.StateMigrations.Finalization;

/// <summary>
/// Ending a manual job. The job row carries the outcome and is what the Module's page reads, so
/// every terminal transition goes through here rather than just transitioning.
/// </summary>
public static class StateMigrationFinalization
{
    public static EventActivityBinder<TSaga, TMessage> ThenJobFailed<TSaga, TMessage>(
        this EventActivityBinder<TSaga, TMessage> binder)
        where TSaga : StateMigrationSagaBase
        where TMessage : class =>
        binder.Activity(x => x.OfType<FailStateMigrationJobActivity<TSaga, TMessage>>());

    public static EventActivityBinder<TSaga, TMessage> ThenJobCompleted<TSaga, TMessage>(
        this EventActivityBinder<TSaga, TMessage> binder)
        where TSaga : StateMigrationSagaBase
        where TMessage : class =>
        binder.Activity(x => x.OfType<CompleteStateMigrationJobActivity<TSaga, TMessage>>());

    public static EventActivityBinder<TSaga, TMessage> ThenJobPartiallyCompleted<TSaga, TMessage>(
        this EventActivityBinder<TSaga, TMessage> binder)
        where TSaga : StateMigrationSagaBase
        where TMessage : class =>
        binder.Activity(x => x.OfType<PartiallyCompleteStateMigrationJobActivity<TSaga, TMessage>>());

    /// <summary>Ends the job as cancelled, whichever message got here.</summary>
    public static EventActivityBinder<TSaga, TMessage> ThenStateMigrationCancelled<TSaga, TMessage>(
        this EventActivityBinder<TSaga, TMessage> binder, State cancelled)
        where TSaga : StateMigrationSagaBase
        where TMessage : class =>
        binder
            .Activity(x => x.OfType<CancelStateMigrationJobActivity<TSaga, TMessage>>())
            .TransitionTo(cancelled)
            .Finalize();

    /// <summary>
    /// Asks the runner to kill what it is running and waits for the answer. Unlike the deployment
    /// families, a state migration has one killable step, so there is no after-current variant:
    /// the request either kills the write or it is already over.
    /// </summary>
    public static EventActivityBinder<TSaga, CancelStateMigrationJobRequested> IfCancelKill<
        TSaga, TCancelKillRequested, TDummyCancelKillCompleted>(
        this EventActivityBinder<TSaga, CancelStateMigrationJobRequested> binder,
        Request<TSaga, TCancelKillRequested, TDummyCancelKillCompleted> cancelKillRequested,
        State cancelling)
        where TSaga : StateMigrationSagaBase
        where TCancelKillRequested : CancelKillRequestedBase, new()
        where TDummyCancelKillCompleted : class =>
        binder
            .Then(context =>
            {
                context.Saga.PreviousStateBeforeCancelling = context.Saga.CurrentState;
                context.Saga.WaitingSince = DateTime.UtcNow;
            })
            .TransitionTo(cancelling)
            .Request(cancelKillRequested,
                context =>
                {
                    if (context.Saga.ServerInstanceId.HasValue)
                        return new Uri(MassTransitHelpers.GetConsumerEndpoint(
                            context.Saga.ServerInstanceId.Value, "CancelKillRequested"));

                    // MassTransit's address-provider lambda allows null to mean "use default address",
                    // even though the declared return type is non-nullable Uri.
#pragma warning disable CS8603
                    return null;
#pragma warning restore CS8603
                },
                context => new TCancelKillRequested
                {
                    OrganizationId = context.Saga.OrganizationId,
                    CorrelationId = context.Saga.CorrelationId,
                    RunnerInstanceName = context.Saga.RunnerInstanceName,
                    RunnerId = context.Saga.RunnerId
                });
}
