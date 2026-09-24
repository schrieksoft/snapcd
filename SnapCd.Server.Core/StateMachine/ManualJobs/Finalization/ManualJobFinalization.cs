// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;

namespace SnapCd.Server.Core.StateMachine.ManualJobs.Finalization;

/// <summary>
/// Ending a manual job. The job row carries the outcome and is what the Module's page reads, so
/// every terminal transition goes through here rather than just transitioning.
/// </summary>
public static class ManualJobFinalization
{
    public static EventActivityBinder<TSaga, TMessage> ThenJobFailed<TSaga, TMessage>(
        this EventActivityBinder<TSaga, TMessage> binder)
        where TSaga : ManualJobSagaBase
        where TMessage : class =>
        binder.Activity(x => x.OfType<FailManualModuleJobActivity<TSaga, TMessage>>());

    public static EventActivityBinder<TSaga, TMessage> ThenJobCompleted<TSaga, TMessage>(
        this EventActivityBinder<TSaga, TMessage> binder)
        where TSaga : ManualJobSagaBase
        where TMessage : class =>
        binder.Activity(x => x.OfType<CompleteManualModuleJobActivity<TSaga, TMessage>>());

    public static EventActivityBinder<TSaga, TMessage> ThenJobPartiallyCompleted<TSaga, TMessage>(
        this EventActivityBinder<TSaga, TMessage> binder)
        where TSaga : ManualJobSagaBase
        where TMessage : class =>
        binder.Activity(x => x.OfType<PartiallyCompleteManualModuleJobActivity<TSaga, TMessage>>());
}
