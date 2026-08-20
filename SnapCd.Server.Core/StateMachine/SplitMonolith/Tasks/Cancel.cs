// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.SplitMonolith;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.SplitMonolith;

public partial class SplitMonolithStateMachine
{
    public Request<SplitMonolithSaga, CancelKillRequested, DummyCancelKillCompleted> CancelKillRequested { get; } = null!;
    public Event<CancelKillCompleted> CancelKillCompleted { get; } = null!;

    public Request<SplitMonolithSaga, CancelGracefulRequested, DummyCancelGracefulCompleted> CancelGracefulRequested { get; } = null!;
    public Event<CancelGracefulCompleted> CancelGracefulCompleted { get; } = null!;

    public Event<CancelModuleRequested> CancelModuleRequested { get; } = null!;

    public State CancellingImmediateKill { get; } = null!;
    public State CancellingImmediateGraceful { get; } = null!;
    public State CancellingAfterCurrent { get; } = null!;
    public State Cancelled { get; } = null!;

    private void Configure_Cancel()
    {
        Event(() => CancelModuleRequested, x => x.CorrelateById(m => m.Message.CorrelationId));

        Request(() => CancelKillRequested, x => x.KillCancellationRequestId, o => { o.Timeout = TimeSpan.FromSeconds(90); });
        Event(() => CancelKillCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));

        Request(() => CancelGracefulRequested, x => x.GracefulCancellationRequestId, o => { o.Timeout = TimeSpan.FromSeconds(90); });
        Event(() => CancelGracefulCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));

        // A step that reports back while cancelling ends the job rather than continuing the chain.
        During(CancellingImmediateKill, CancelHandlers());
        During(CancellingImmediateGraceful, CancelHandlers());
        During(CancellingAfterCurrent, CancelHandlers());

        During(Cancelled,
            Ignore(RunnerReconnectedEvent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );
    }

    private EventActivities<SplitMonolithSaga>[] CancelHandlers() =>
    [
        When(SelectRunnerInstanceCompleted).ThenSplitCancelled(Cancelled),
        When(GetModuleCompleted).ThenSplitCancelled(Cancelled),
        When(InitCompleted).ThenSplitCancelled(Cancelled),
        When(ValidateCompleted).ThenSplitCancelled(Cancelled),
        When(PlanCompleted).ThenSplitCancelled(Cancelled),
        When(PlanEmptyVerifyCompleted).ThenSplitCancelled(Cancelled),
        When(RefactorVerifyCompleted).ThenSplitCancelled(Cancelled),
        When(MigrateMapCompleted).ThenSplitCancelled(Cancelled),
        When(MigrateProveCompleted).ThenSplitCancelled(Cancelled),
        When(MigrateRunCompleted).ThenSplitCancelled(Cancelled),
        When(MigrateVerifyCompleted).ThenSplitCancelled(Cancelled),
        When(CancelKillCompleted).ThenSplitCancelled(Cancelled),
        When(CancelGracefulCompleted).ThenSplitCancelled(Cancelled),
        Ignore(RunnerReconnectedEvent),
        Ignore(HeartbeatScheduled.Received),
        Ignore(HeartbeatRequested.Completed),
        Ignore(HeartbeatRequested.Completed2)
    ];
}
