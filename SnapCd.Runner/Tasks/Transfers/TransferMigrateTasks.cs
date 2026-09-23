// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.RunnerRequests.Transfers;
using SnapCd.Runner.Services.SplitMigrate;
using SnapCd.Runner.Services.Transfers;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    /// <summary>
    /// Writes this Module's share of the move into its own state. The receiver injects the moved
    /// resources; the source strips them, and demonolith refuses to strip until the receiver's run
    /// receipt is in its checkout - committed and merged, like the map.
    /// </summary>
    public Task TransferMigrateRun(TransferMigrateRunRequestBase request, HubConnection connection) =>
        RunTransferStep(request.JobId, request.ModuleId, nameof(TransferMigrateRun), request.Metadata,
            request.ReportActiveJobFrequencySeconds, connection,
            async (taskContext, client, killToken, gracefulToken) =>
            {
                taskContext.LogNarration("Now running demonolith transfer migrate run");

                var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);


                var command = DemonolithCommand.Build("transfer migrate run", request.RootDirectory, request.Engine);

                await engine.RunProcess(command, killToken, gracefulToken);

                await InvokeWithRetryAsync(
                    () => client.InvokeTransferMigrateRunCompleted(request.JobId, request.ModuleId),
                    nameof(client.InvokeTransferMigrateRunCompleted), request.JobId, connection);
            },
            (client, message, stackTrace) =>
                client.InvokeTransferMigrateRunFaulted(request.JobId, request.ModuleId, message, stackTrace));

    /// <summary>Checks the written state plans clean, which closes this Module's move.</summary>
    public Task TransferMigrateVerify(TransferMigrateVerifyRequestBase request, HubConnection connection) =>
        RunTransferStep(request.JobId, request.ModuleId, nameof(TransferMigrateVerify), request.Metadata,
            request.ReportActiveJobFrequencySeconds, connection,
            async (taskContext, client, killToken, gracefulToken) =>
            {
                taskContext.LogNarration("Now running demonolith transfer migrate verify");

                var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);


                var command = DemonolithCommand.Build("transfer migrate verify", request.RootDirectory, request.Engine);

                await engine.RunProcess(command, killToken, gracefulToken);

                await InvokeWithRetryAsync(
                    () => client.InvokeTransferMigrateVerifyCompleted(request.JobId, request.ModuleId),
                    nameof(client.InvokeTransferMigrateVerifyCompleted), request.JobId, connection);
            },
            (client, message, stackTrace) =>
                client.InvokeTransferMigrateVerifyFaulted(request.JobId, request.ModuleId, message, stackTrace));
}
