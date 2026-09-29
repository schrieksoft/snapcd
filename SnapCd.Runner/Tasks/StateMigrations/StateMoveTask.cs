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
    /// <summary>
    /// What a move would do, asked of the engine itself: state mv -dry-run prints what it would
    /// move and moves nothing, so the check runs the same command and the same address parsing as
    /// the move that follows it.
    /// </summary>
    public Task MoveDryRun(StateMoveRequestBase request, HubConnection connection) =>
        RunTransferStep(request.JobId, Guid.Empty, RunnerEndpoints.MoveDryRun, request.Metadata,
            request.ReportActiveJobFrequencySeconds, connection,
            async (taskContext, client, killToken) =>
            {
                taskContext.LogNarration(
                    $"Checking what a move over {request.Instructions.Count} addresses would do");
                taskContext.LogBreak();

                var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);

                var outcomes = await engine.Move(
                    request.Instructions.Select(i => (i.Address, i.Target)).ToList(),
                    dryRun: true, killToken);

                var results = Results(outcomes, request);

                await InvokeWithRetryAsync(
                    () => client.InvokeMoveDryRunCompleted(request.JobId, results),
                    nameof(client.InvokeMoveDryRunCompleted), request.JobId, connection);
            },
            (client, message, stackTrace) =>
                client.InvokeMoveDryRunFaulted(request.JobId, message, stackTrace));

    /// <summary>
    /// What a remove would take out, asked of the engine itself. state rm -dry-run prints what it
    /// would remove and removes nothing.
    /// </summary>
    public Task RemoveDryRun(StateMoveRequestBase request, HubConnection connection) =>
        RunTransferStep(request.JobId, Guid.Empty, RunnerEndpoints.RemoveDryRun, request.Metadata,
            request.ReportActiveJobFrequencySeconds, connection,
            async (taskContext, client, killToken) =>
            {
                taskContext.LogNarration(
                    $"Checking what removing {request.Instructions.Count} addresses would take out");
                taskContext.LogBreak();

                var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);

                var outcomes = await engine.Remove(
                    request.Instructions.Select(i => i.Address).ToList(),
                    dryRun: true, killToken);

                var results = Results(outcomes, request);

                await InvokeWithRetryAsync(
                    () => client.InvokeRemoveDryRunCompleted(request.JobId, results),
                    nameof(client.InvokeRemoveDryRunCompleted), request.JobId, connection);
            },
            (client, message, stackTrace) =>
                client.InvokeRemoveDryRunFaulted(request.JobId, message, stackTrace));

    /// <summary>
    /// An import has no dry run, so this is a different question answered a different way: whether
    /// each address it would create is free, since an import onto an occupied address fails.
    /// Whether an id names a real resource is only answerable by importing it, so it is not asked.
    /// </summary>
    public Task ImportPreCheck(StateMoveRequestBase request, HubConnection connection) =>
        RunTransferStep(request.JobId, Guid.Empty, RunnerEndpoints.ImportPreCheck, request.Metadata,
            request.ReportActiveJobFrequencySeconds, connection,
            async (taskContext, client, killToken) =>
            {
                taskContext.LogNarration(
                    $"Checking whether {request.Instructions.Count} addresses are free to import onto");
                taskContext.LogBreak();

                var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);

                var (present, _) = await engine.StateListFiltered(
                    request.Instructions.Select(i => i.Address).ToList(), killToken);

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

                await InvokeWithRetryAsync(
                    () => client.InvokeImportPreCheckCompleted(request.JobId, results),
                    nameof(client.InvokeImportPreCheckCompleted), request.JobId, connection);
            },
            (client, message, stackTrace) =>
                client.InvokeImportPreCheckFaulted(request.JobId, message, stackTrace));

    /// <summary>Moves each address to where it should be. One command per address.</summary>
    public Task Move(StateMoveRequestBase request, HubConnection connection) =>
        RunTransferStep(request.JobId, Guid.Empty, RunnerEndpoints.StateMove, request.Metadata,
            request.ReportActiveJobFrequencySeconds, connection,
            async (taskContext, client, killToken) =>
            {
                taskContext.LogNarration($"Moving {request.Instructions.Count} addresses");
                taskContext.LogBreak();

                var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);

                var outcomes = await engine.Move(
                    request.Instructions.Select(i => (i.Address, i.Target)).ToList(),
                    dryRun: false, killToken);

                await InvokeWithRetryAsync(
                    () => client.InvokeMoveCompleted(request.JobId, Results(outcomes, request)),
                    nameof(client.InvokeMoveCompleted), request.JobId, connection);
            },
            (client, message, stackTrace) =>
                client.InvokeMoveFaulted(request.JobId, message, stackTrace));

    /// <summary>Imports each address from the id it already has. One command per address.</summary>
    public Task Import(StateMoveRequestBase request, HubConnection connection) =>
        RunTransferStep(request.JobId, Guid.Empty, RunnerEndpoints.StateImport, request.Metadata,
            request.ReportActiveJobFrequencySeconds, connection,
            async (taskContext, client, killToken) =>
            {
                taskContext.LogNarration($"Importing {request.Instructions.Count} addresses");
                taskContext.LogBreak();

                var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);

                var outcomes = await engine.Import(
                    request.Instructions.Select(i => (i.Address, i.Target)).ToList(),
                    killToken);

                await InvokeWithRetryAsync(
                    () => client.InvokeMoveCompleted(request.JobId, Results(outcomes, request)),
                    nameof(client.InvokeMoveCompleted), request.JobId, connection);
            },
            (client, message, stackTrace) =>
                client.InvokeMoveFaulted(request.JobId, message, stackTrace));

    /// <summary>Takes each address out of state, leaving the infrastructure alone.</summary>
    public Task Remove(StateMoveRequestBase request, HubConnection connection) =>
        RunTransferStep(request.JobId, Guid.Empty, RunnerEndpoints.StateRemove, request.Metadata,
            request.ReportActiveJobFrequencySeconds, connection,
            async (taskContext, client, killToken) =>
            {
                taskContext.LogNarration($"Removing {request.Instructions.Count} addresses from state");
                taskContext.LogBreak();

                var engine = _engineFactory.Create(taskContext, request.Engine, request.Metadata);

                var outcomes = await engine.Remove(
                    request.Instructions.Select(i => i.Address).ToList(),
                    dryRun: false, killToken);

                await InvokeWithRetryAsync(
                    () => client.InvokeMoveCompleted(request.JobId, Results(outcomes, request)),
                    nameof(client.InvokeMoveCompleted), request.JobId, connection);
            },
            (client, message, stackTrace) =>
                client.InvokeMoveFaulted(request.JobId, message, stackTrace));

    /// <summary>What each address ended up as, with the target it was given.</summary>
    private static List<StateAddressResult> Results(
        List<(string Address, bool Succeeded)> outcomes, StateMoveRequestBase request)
    {
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
}
