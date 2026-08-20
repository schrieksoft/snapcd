// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.SplitMonolith;
using SnapCd.Contracts.RunnerRequests;

namespace SnapCd.Server.Core.StateMachine.SplitMonolith;

public partial class SplitMonolithStateMachine
{
    public Event<RefactorVerifyCompleted> RefactorVerifyCompleted { get; } = null!;
    public Event<RefactorVerifyCancelled> RefactorVerifyCancelled { get; } = null!;
    public Event<RefactorVerifyFaulted> RefactorVerifyFaulted { get; } = null!;

    public State RefactorVerifyPending { get; } = null!;
    public State RefactorVerifyWaitingForRunner { get; } = null!;

    private void Configure_RefactorVerify()
    {
        Event(() => RefactorVerifyCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => RefactorVerifyCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => RefactorVerifyFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));

        CreateStep<RefactorVerifyCompleted, RefactorVerifyCancelled, RefactorVerifyFaulted, MigrateMapRequested>(
            RefactorVerifyWaitingForRunner,
            RefactorVerifyPending,
            RefactorVerifyCompleted,
            RefactorVerifyCancelled,
            RefactorVerifyFaulted,
            MigrateMapWaitingForRunner,
            MigrateMapPending
        );
    }
}
