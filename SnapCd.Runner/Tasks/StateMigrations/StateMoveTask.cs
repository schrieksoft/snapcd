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

                await ReportAddressOutcomes(
                    taskContext, connection, request.JobId, results,
                    () => client.InvokeMoveDryRunCompleted(request.JobId, results),
                    nameof(client.InvokeMoveDryRunCompleted),
                    summary => client.InvokeMoveDryRunFaulted(request.JobId, summary, null),
                    nameof(client.InvokeMoveDryRunFaulted),
                    "Move");
            },
            (client, message, stackTrace) =>
                client.InvokeMoveDryRunFaulted(request.JobId, message, stackTrace),
            client => client.InvokeMoveDryRunCancelled(request.JobId));

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

                await ReportAddressOutcomes(
                    taskContext, connection, request.JobId, results,
                    () => client.InvokeRemoveDryRunCompleted(request.JobId, results),
                    nameof(client.InvokeRemoveDryRunCompleted),
                    summary => client.InvokeRemoveDryRunFaulted(request.JobId, summary, null),
                    nameof(client.InvokeRemoveDryRunFaulted),
                    "Remove");
            },
            (client, message, stackTrace) =>
                client.InvokeRemoveDryRunFaulted(request.JobId, message, stackTrace),
            client => client.InvokeRemoveDryRunCancelled(request.JobId));

    /// <summary>
    /// An import has no dry run, so this is a different question answered a different way: whether
    /// each address it would create is free, since an import onto an occupied address fails.
    /// Whether the configuration declares the address, and whether its type can be imported at
    /// all, are the engine's to answer and only the import itself asks them.
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

                foreach (var instruction in request.Instructions)
                {
                    var free = !occupied.Contains(instruction.Address);

                    taskContext.LogInformation(free
                        ? $"  {Ansi.Emphasis(instruction.Address)} from {Ansi.Emphasis(instruction.Target ?? "")}"
                        : $"  {Ansi.Emphasis(instruction.Address)} is already in state, so there is nothing to import onto");

                    results.Add(new StateAddressResult
                    {
                        Address = instruction.Address,
                        Target = instruction.Target,
                        Outcome = free ? "Succeeded" : "Failed"
                    });
                }

                // Only the engine knows whether an address is declared or its type can be imported,
                // and import has no dry run, so those are answered by the import itself.
                taskContext.LogBreak();
                taskContext.LogNarration(
                    "This checks the addresses are free. The import can still fail if the "
                    + "configuration does not declare one, or its resource type cannot be imported.");

                await ReportAddressOutcomes(
                    taskContext, connection, request.JobId, results,
                    () => client.InvokeImportPreCheckCompleted(request.JobId, results),
                    nameof(client.InvokeImportPreCheckCompleted),
                    summary => client.InvokeImportPreCheckFaulted(request.JobId, summary, null),
                    nameof(client.InvokeImportPreCheckFaulted),
                    "Import");
            },
            (client, message, stackTrace) =>
                client.InvokeImportPreCheckFaulted(request.JobId, message, stackTrace),
            client => client.InvokeImportPreCheckCancelled(request.JobId));

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

                var results = Results(outcomes, request);

                await ReportAddressOutcomes(
                    taskContext, connection, request.JobId, results,
                    () => client.InvokeMoveCompleted(request.JobId, results),
                    nameof(client.InvokeMoveCompleted),
                    summary => client.InvokeMoveFaulted(request.JobId, summary, null),
                    nameof(client.InvokeMoveFaulted),
                    "Move");
            },
            (client, message, stackTrace) =>
                client.InvokeMoveFaulted(request.JobId, message, stackTrace),
            client => client.InvokeMoveCancelled(request.JobId));

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

                var results = Results(outcomes, request);

                await ReportAddressOutcomes(
                    taskContext, connection, request.JobId, results,
                    () => client.InvokeImportCompleted(request.JobId, results),
                    nameof(client.InvokeImportCompleted),
                    summary => client.InvokeImportFaulted(request.JobId, summary, null),
                    nameof(client.InvokeImportFaulted),
                    "Import");
            },
            (client, message, stackTrace) =>
                client.InvokeImportFaulted(request.JobId, message, stackTrace),
            client => client.InvokeImportCancelled(request.JobId));

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

                var results = Results(outcomes, request);

                await ReportAddressOutcomes(
                    taskContext, connection, request.JobId, results,
                    () => client.InvokeRemoveCompleted(request.JobId, results),
                    nameof(client.InvokeRemoveCompleted),
                    summary => client.InvokeRemoveFaulted(request.JobId, summary, null),
                    nameof(client.InvokeRemoveFaulted),
                    "Remove");
            },
            (client, message, stackTrace) =>
                client.InvokeRemoveFaulted(request.JobId, message, stackTrace),
            client => client.InvokeRemoveCancelled(request.JobId));

    /// <summary>What each address ended up as, with the target it was given.</summary>
    /// <summary>
    /// Reports a step's outcome the way <c>SplitPlanEmptyVerify</c> does: a refusal is the engine
    /// answering, not an exception, so the runner decides and faults rather than reporting success
    /// and leaving the server to read a result set it does not inspect.
    /// </summary>
    private async Task ReportAddressOutcomes(
        RunnerTaskContext taskContext,
        HubConnection connection,
        Guid jobId,
        List<StateAddressResult> results,
        Func<Task> completed,
        string completedName,
        Func<string, Task> faulted,
        string faultedName,
        string verb)
    {
        var refused = results.Where(r => r.Outcome == "Failed").Select(r => r.Address).ToList();

        if (refused.Count == 0)
        {
            await InvokeWithRetryAsync(completed, completedName, jobId, connection);
            return;
        }

        var summary = refused.Count == results.Count
            ? $"{verb} failed for every address: {string.Join(", ", refused)}. Nothing was changed."
            : $"{verb} failed for {string.Join(", ", refused)}. "
              + $"{results.Count - refused.Count} of {results.Count} addresses were managed.";

        taskContext.LogError(summary);

        await InvokeWithRetryAsync(() => faulted(summary), faultedName, jobId, connection);
    }

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
