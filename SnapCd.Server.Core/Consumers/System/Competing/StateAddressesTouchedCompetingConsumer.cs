// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Crud.Transfers;

namespace SnapCd.Server.Core.Consumers.System.Competing;

/// <summary>
/// Closes the transfer ledger from what a job saw in a Module's state. Any state-touching job
/// reaches here, so an address moved by hand accounts for itself.
/// </summary>
public class StateAddressesTouchedCompetingConsumer(TransferLedger ledger)
    : IConsumer<StateAddressesTouched>
{
    public async Task Consume(ConsumeContext<StateAddressesTouched> context)
    {
        var message = context.Message;

        var touched = await ledger.Account(
            message.ModuleId, message.OrganizationId, message.JobId, message.Present, message.Absent);

        foreach (var transferId in touched)
            await ledger.CloseIfAccountedFor(transferId, message.OrganizationId);
    }
}
