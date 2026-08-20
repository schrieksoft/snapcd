// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.Steps.SplitMonolith;
using SnapCd.Server.Core.Services.MaintenanceMode;
using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.StateMachine.SplitMonolith.Activites;

namespace SnapCd.Server.Core.StateMachine.SplitMonolith.Activites;

/// <summary>
/// Sends a split's step requests, carrying the operator's choices from the saga onto the requests
/// that use them. The base activity sets only the fields every job kind has.
/// </summary>
public class SendSplitStepToRunnerActivity<TMessage, TOutgoingMessage>
    : SendToRunnerActivity<SplitMonolithSaga, TMessage, TOutgoingMessage>
    where TMessage : class
    where TOutgoingMessage : StepRequestBase, new()
{
    public SendSplitStepToRunnerActivity(
        SnapCdDbContext dbContext,
        IMaintenanceModeService maintenanceMode,
        ILogger<SendToRunnerActivity<SplitMonolithSaga, TMessage, TOutgoingMessage>> logger)
        : base(dbContext, maintenanceMode, logger)
    {
    }

    protected override TOutgoingMessage CreateMessage(SplitMonolithSaga saga)
    {
        var message = base.CreateMessage(saga);

        if (message is SplitStepRequestBase split)
        {
            split.RootDirectory = saga.RootDirectory;
        }

        // Only the push can replace a destination, so only its request carries the flag.
        if (message is MigrateRunRequested run)
            run.Overwrite = saga.Overwrite;

        return message;
    }
}
