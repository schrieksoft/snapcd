// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.AspNetCore.SignalR.Client;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.RunnerRequests.StateMigrations;
using SnapCd.Runner.Services;

namespace SnapCd.Runner.Tasks;

public partial class Tasks
{
    /// <summary>
    /// What the edit would do, run before anyone is asked to approve it. A move and a remove ask
    /// the engine itself, which reports what it would touch without touching it. An import has no
    /// such mode, so the check is that each address is free: an import onto an occupied address
    /// fails, and that is the precondition worth knowing before approving rather than after.
    /// </summary>
    public Task StatePreCheck(StateMoveRequestBase request, HubConnection connection) =>
        RunTransferStep(request.JobId, Guid.Empty, PreCheckTaskName(request.Operation), request.Metadata,
            request.ReportActiveJobFrequencySeconds, connection,
            async (taskContext, client, killToken, gracefulToken) =>
            {
                var operation = Enum.Parse<StateMoveOperation>(request.Operation);
                var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);

                taskContext.LogNarration(
                    $"Checking what {operation} would do over {request.Instructions.Count} addresses");
                taskContext.LogBreak();

                var results = operation == StateMoveOperation.Import
                    ? await PreCheckImport(taskContext, engine, request, killToken, gracefulToken)
                    : await PreCheckWithDryRun(operation, engine, request, killToken, gracefulToken);

                await InvokeWithRetryAsync(
                    () => client.InvokeStatePreCheckCompleted(request.JobId, request.Operation, results),
                    nameof(client.InvokeStatePreCheckCompleted), request.JobId, connection);
            },
            (client, message, stackTrace) =>
                client.InvokeStatePreCheckFaulted(request.JobId, message, stackTrace));

    /// <summary>The step's name as the job records it, which is the endpoint it arrived on.</summary>
    private static string PreCheckTaskName(string operation) =>
        Enum.Parse<StateMoveOperation>(operation) switch
        {
            StateMoveOperation.Move => RunnerEndpoints.MoveDryRun,
            StateMoveOperation.Remove => RunnerEndpoints.RemoveDryRun,
            StateMoveOperation.Import => RunnerEndpoints.ImportPreCheck,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

    /// <summary>The engine's own dry run: it prints what it would move or remove, and moves nothing.</summary>
    private static async Task<List<StateAddressResult>> PreCheckWithDryRun(
        StateMoveOperation operation,
        IEngine engine,
        StateMoveRequestBase request,
        CancellationToken killToken,
        CancellationToken gracefulToken)
    {
        var outcomes = await engine.StateMove(
            operation,
            request.Instructions.Select(i => (i.Address, i.Target)).ToList(),
            dryRun: true,
            killToken,
            gracefulToken);

        var targets = request.Instructions.ToDictionary(i => i.Address, i => i.Target);

        return outcomes
            .Select(o => new StateAddressResult
            {
                Address = o.Address,
                Target = targets.GetValueOrDefault(o.Address),
                Outcome = o.Succeeded ? "Succeeded" : "Failed"
            })
            .ToList();
    }

    /// <summary>
    /// An import has no dry run, so what can be checked is that the address it would create is
    /// free. Whether the id names a real resource is only answerable by importing it.
    /// </summary>
    private static async Task<List<StateAddressResult>> PreCheckImport(
        RunnerTaskContext taskContext,
        IEngine engine,
        StateMoveRequestBase request,
        CancellationToken killToken,
        CancellationToken gracefulToken)
    {
        var addresses = request.Instructions.Select(i => i.Address).ToList();
        var (present, _) = await engine.StateListFiltered(addresses, killToken, gracefulToken);

        var occupied = present.ToHashSet();
        var results = new List<StateAddressResult>();

        taskContext.LogInformation("Would import");

        foreach (var instruction in request.Instructions)
        {
            var free = !occupied.Contains(instruction.Address);

            taskContext.LogInformation(free
                ? $"  {Ansi.Emphasis(instruction.Address)} from {Ansi.Emphasis(instruction.Target ?? "")}"
                : $"  {Ansi.Emphasis(instruction.Address)} is already in state and cannot be imported onto");

            results.Add(new StateAddressResult
            {
                Address = instruction.Address,
                Target = instruction.Target,
                Outcome = free ? "Succeeded" : "Failed"
            });
        }

        return results;
    }

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
                taskContext.LogBreak();

                var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);

                var outcomes = await engine.StateMove(
                    operation,
                    request.Instructions.Select(i => (i.Address, i.Target)).ToList(),
                    dryRun: false,
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
