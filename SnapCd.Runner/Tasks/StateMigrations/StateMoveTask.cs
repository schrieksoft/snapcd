// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.RunnerRequests.StateMigrations;
using SnapCd.Runner.Services;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    /// <summary>
    /// Moves, imports or removes a batch of addresses. Each runs on its own, so a batch of five
    /// that manages three reports exactly that rather than failing whole.
    /// </summary>
    public Task StateMove(StateMoveRequestBase request, HubConnection connection) =>
        RunTransferStep(request.JobId, Guid.Empty, request.Operation, request.Metadata,
            request.ReportActiveJobFrequencySeconds, connection,
            async (taskContext, client, killToken, gracefulToken) =>
            {
                var operation = Enum.Parse<StateMoveOperation>(request.Operation);

                taskContext.LogNarration(
                    $"Running {operation} over {request.Instructions.Count} addresses");

                var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);

                var outcomes = await engine.StateMove(
                    operation,
                    request.Instructions.Select(i => (i.Address, i.Target)).ToList(),
                    killToken,
                    gracefulToken);

                var targets = request.Instructions.ToDictionary(i => i.Address, i => i.Target);

                var results = outcomes
                    .Select(o => new StateAddressResult
                    {
                        Address = o.Address,
                        Target = targets.GetValueOrDefault(o.Address),
                        Outcome = o.Succeeded ? "Succeeded" : "Failed"
                    })
                    .ToList();

                await InvokeWithRetryAsync(
                    () => client.InvokeStateMoveCompleted(request.JobId, request.Operation, results),
                    nameof(client.InvokeStateMoveCompleted), request.JobId, connection);
            },
            (client, message, stackTrace) =>
                client.InvokeStateMoveFaulted(request.JobId, message, stackTrace));
}
