// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Endpoints;
using SnapCd.Contracts.RunnerRequests.Transfers;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Contracts;
using SnapCd.Contracts.Clients;
using SnapCd.Contracts.Constants;
using Serilog.Events;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Contracts.Dto.Outputs;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Contracts.RunnerRequests.StateMigrations;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Server.Core.Hubs;

namespace SnapCd.JobRun;

/// <summary>
/// Answers dispatches as a runner would, without a runner. Each reply goes back through the real
/// hub method, so the routing and authorization a live runner would exercise are exercised here
/// too; only the work behind the reply is canned.
/// </summary>
public class FakeRunner(
    IServiceProvider services,
    string connectionId,
    Guid organizationId,
    string instanceName,
    Action<string> trace)
{
    /// <summary>The pool the run registered against, known only once the Module is read.</summary>
    public Guid RunnerId { get; set; }

    /// <summary>The Module the logs are attributed to.</summary>
    public Guid ModuleId { get; set; }

    private readonly SemaphoreSlim _reporting = new(1, 1);
    private Guid _liveJobId;
    private string _liveTask = "";

    /// <summary>
    /// Which endpoint, if any, should hold rather than answer, standing in for a command still
    /// running. A real kill releases it, which is the only way to test a cancel mid-step: a step
    /// that answers at once is always over before the cancel arrives.
    /// </summary>
    public string? HoldAt { get; set; }

    private readonly CancellationTokenSource _killed = new();

    /// <summary>Set once the server asked this runner to kill what it is running.</summary>
    public bool WasKilled { get; private set; }

    private readonly List<string> _dispatched = [];

    public IReadOnlyList<string> Dispatched
    {
        get { lock (_dispatched) return _dispatched.ToList(); }
    }

    /// <summary>
    /// Reported by the canned plan. A plan with nothing to change routes to Output instead of
    /// apply, so a run meant to reach apply needs this above zero.
    /// </summary>
    public int ChangedCount { get; set; } = 1;

    public PolicyOutcome PolicyOutcome { get; set; } = PolicyOutcome.Passed;

    /// <summary>Set for a transfer, which refuses to move state a plan says is not settled.</summary>
    public bool PlansClean { get; set; }

    /// <summary>What a split's map claims to have carved out, and what its proof then covers.</summary>
    public string CarvedModuleName { get; set; } = "carved";

    public int ModulesProven { get; set; } = 1;

    /// <summary>
    /// The Module whose plan reads a value the other produces, and the outputs it waits for. Only
    /// that side reports needing anything, so only it parks; the other passes straight through
    /// and publishes what the first is waiting for.
    /// </summary>
    public Guid ConsumingModuleId { get; set; }

    /// <summary>The half that gives the resources away, and so cuts the fragment.</summary>
    public Guid SourceModuleId { get; set; }

    public IReadOnlyCollection<string> NeedsOutputs { get; set; } = [];

    /// <summary>The addresses a transfer's run reports having moved.</summary>
    public List<string> TransferredAddresses { get; set; } =
        ["random_pet.dns_zone", "random_uuid.dns_zone_id"];

    public async Task Handle(string endpoint, object payload)
    {
        lock (_dispatched) _dispatched.Add(endpoint);
        trace($"  -> runner received {endpoint}");

        // Resolved per reply: the hub is scoped, and each reply is its own unit of work.
        await using var scope = services.CreateAsyncScope();
        var hub = scope.ServiceProvider.GetRequiredService<RunnerHub>();
        hub.Context = new FakeRunnerContext(connectionId, organizationId);

        // The liveness ping carries a bare id rather than a step request, and a connection that
        // does not answer it is dropped as stale before any step is dispatched.
        if (endpoint == nameof(IRunnerLifecycleEndpoints.Ping))
        {
            await hub.Pong((Guid)payload);
            return;
        }

        var jobId = Read<Guid>(payload, "JobId");

        // A real runner reports its task on a timer for as long as it runs, which is what the
        // heartbeat reads to decide the runner is still there. Reporting once is not enough: the
        // job idles between steps and is failed as abandoned once the threshold passes.
        _liveJobId = jobId;
        _liveTask = endpoint;

        // A real runner streams its output as it works, and the page shows a job's logs from
        // these. Without them a running job reads as one doing nothing.
        await SendLog(hub, jobId, endpoint);

        // One report at a time: the row is unique per job, so a timer tick landing on top of a
        // step's own report collides.
        await _reporting.WaitAsync();
        try
        {
            await hub.ReportRunningTask(jobId, endpoint, RunnerId, instanceName);
        }
        finally
        {
            _reporting.Release();
        }

        // Stands in for a command that is still running: the step answers only once the kill
        // arrives, so a cancel sent meanwhile lands while the step is genuinely in flight.
        if (endpoint == HoldAt)
        {
            trace($"  -> holding at {endpoint} until killed");
            try
            {
                await Task.Delay(Timeout.Infinite, _killed.Token);
            }
            catch (OperationCanceledException)
            {
                trace($"  -> {endpoint} released by the kill");
            }
        }

        // The same retry the real runner uses, so a reply that beats the saga's transition is
        // answered here exactly as it would be in production.
        await HubInvocationRetry.InvokeAsync(
            () => Reply(hub, endpoint, payload, jobId),
            onAttemptFailed: (ex, attempt, delay) =>
            {
                trace($"  -> {endpoint} rejected (attempt {attempt}): {ex.Message}");
                return Task.FromResult(delay);
            });
    }

    /// <summary>Acknowledges the kill on the endpoint the asking family listens on.</summary>
    private static async Task ReportKilled(RunnerHub hub, string endpoint, Guid jobId)
    {
        switch (endpoint)
        {
            case nameof(IMoveEndpoints.MoveCancelKill): await hub.MoveCancelKillCompleted(jobId); break;
            case nameof(IImportEndpoints.ImportCancelKill): await hub.ImportCancelKillCompleted(jobId); break;
            case nameof(IRemoveEndpoints.RemoveCancelKill): await hub.RemoveCancelKillCompleted(jobId); break;
            case nameof(IApplyEndpoints.ApplyCancelKill): await hub.ApplyCancelKillCompleted(jobId); break;
            case nameof(IDestroyEndpoints.DestroyCancelKill): await hub.DestroyCancelKillCompleted(jobId); break;
            case nameof(ISplitEndpoints.SplitCancelKill): await hub.SplitCancelKillCompleted(jobId); break;
        }
    }

    /// <summary>The canned answer for one dispatched endpoint.</summary>
    private async Task Reply(RunnerHub hub, string endpoint, object payload, Guid jobId)
    {
        switch (endpoint)
        {
            case nameof(IMoveEndpoints.MoveCancelKill):
            case nameof(IImportEndpoints.ImportCancelKill):
            case nameof(IRemoveEndpoints.RemoveCancelKill):
            case nameof(IApplyEndpoints.ApplyCancelKill):
            case nameof(IDestroyEndpoints.DestroyCancelKill):
            case nameof(ISplitEndpoints.SplitCancelKill):
                WasKilled = true;
                await _killed.CancelAsync();
                await ReportKilled(hub, endpoint, jobId);
                break;

            case nameof(IApplyEndpoints.ApplyGetDefinitiveRevision):
                await hub.ApplyGetDefinitiveRevisionCompleted(jobId, "0000000000000000000000000000000000000000");
                break;
            case nameof(IDestroyEndpoints.DestroyGetDefinitiveRevision):
                await hub.DestroyGetDefinitiveRevisionCompleted(jobId, "0000000000000000000000000000000000000000");
                break;
            // Apply and destroy answer on their own endpoints, as every other family does.
            case nameof(IApplyEndpoints.ApplyGetModule):
                await hub.ApplyGetModuleCompleted(jobId);
                break;
            case nameof(IApplyEndpoints.ApplyInit):
                await hub.ApplyInitCompleted(jobId);
                break;
            case nameof(IApplyEndpoints.ApplyValidate):
                await hub.ApplyValidateCompleted(jobId);
                break;
            case nameof(IApplyEndpoints.ApplyVariables):
                await hub.ApplyVariablesCompleted(jobId, null);
                break;
            case nameof(IDestroyEndpoints.DestroyGetModule):
                await hub.DestroyGetModuleCompleted(jobId);
                break;
            case nameof(IDestroyEndpoints.DestroyInit):
                await hub.DestroyInitCompleted(jobId);
                break;
            case nameof(IDestroyEndpoints.DestroyValidate):
                await hub.DestroyValidateCompleted(jobId);
                break;
            case nameof(IDestroyEndpoints.DestroyVariables):
                await hub.DestroyVariablesCompleted(jobId, null);
                break;

            // The manual families run the same checkout and init, each answering on its own
            // endpoint, so each is replied to by name.
            case nameof(ILookupAddressesEndpoints.LookupAddressesGetModule):
                await hub.LookupAddressesGetModuleCompleted(jobId);
                break;
            case nameof(ILookupAddressesEndpoints.LookupAddressesInit):
                await hub.LookupAddressesInitCompleted(jobId);
                break;
            case nameof(IMoveEndpoints.MoveGetModule):
                await hub.MoveGetModuleCompleted(jobId);
                break;
            case nameof(IMoveEndpoints.MoveInit):
                await hub.MoveInitCompleted(jobId);
                break;
            case nameof(IImportEndpoints.ImportGetModule):
                await hub.ImportGetModuleCompleted(jobId);
                break;
            case nameof(IImportEndpoints.ImportInit):
                await hub.ImportInitCompleted(jobId);
                break;
            case nameof(IRemoveEndpoints.RemoveGetModule):
                await hub.RemoveGetModuleCompleted(jobId);
                break;
            case nameof(IRemoveEndpoints.RemoveInit):
                await hub.RemoveInitCompleted(jobId);
                break;
            case nameof(IApplyEndpoints.ApplyPolicyValidate):
                await hub.ApplyPolicyValidateCompleted(jobId, PolicyOutcome);
                break;
            case nameof(IDestroyEndpoints.DestroyPolicyValidate):
                await hub.DestroyPolicyValidateCompleted(jobId, PolicyOutcome);
                break;
            case nameof(IApplyEndpoints.ApplyPlan):
            {
                // A transfer needs a clean plan to go ahead, while an apply with nothing to
                // change routes to Output instead and never reaches the apply.
                var changed = PlansClean ? 0 : ChangedCount;

                await hub.ApplyPlanCompleted(jobId, new PlanCompletedData
                {
                    TotalChangedCount = changed,
                    CreateCount = changed,
                    TotalCountAfter = changed,
                    PolicyOutcome = PolicyOutcome
                });
                break;
            }
            case nameof(ISplitEndpoints.SplitGetModule):
                await hub.SplitGetModuleCompleted(jobId);
                break;
            case nameof(ISplitEndpoints.SplitInit):
                await hub.SplitInitCompleted(jobId);
                break;
            case nameof(ISplitEndpoints.SplitValidate):
                await hub.SplitValidateCompleted(jobId);
                break;
            case nameof(ISplitEndpoints.SplitPlan):
                await hub.SplitPlanCompleted(jobId, new PlanCompletedData
                {
                    TotalChangedCount = ChangedCount,
                    CreateCount = ChangedCount,
                    TotalCountAfter = ChangedCount,
                    PolicyOutcome = PolicyOutcome
                });
                break;
            case nameof(ISplitEndpoints.SplitPlanEmptyVerify):
                await hub.SplitPlanEmptyVerifyCompleted(jobId);
                break;
            case nameof(IApplyEndpoints.ApplyFromPlan):
                await hub.ApplyFromPlanCompleted(jobId, ChangedCount);
                break;

            // A destroy plans what it would remove, then removes it, leaving nothing in state.
            case nameof(IDestroyEndpoints.DestroyPlan):
                await hub.DestroyPlanCompleted(jobId, new PlanCompletedData
                {
                    TotalChangedCount = ChangedCount,
                    DestroyCount = ChangedCount,
                    TotalCountAfter = 0,
                    PolicyOutcome = PolicyOutcome
                });
                break;
            case nameof(IDestroyEndpoints.DestroyFromPlan):
                await hub.DestroyFromPlanCompleted(jobId, 0);
                break;
            case nameof(ITransferEndpoints.TransferGetModule):
                await hub.TransferGetModuleCompleted(jobId, Read<Guid>(payload, "ModuleId"));
                break;
            case nameof(ITransferEndpoints.TransferInit):
                await hub.TransferInitCompleted(jobId, Read<Guid>(payload, "ModuleId"));
                break;
            case nameof(ITransferEndpoints.TransferValidate):
                await hub.TransferValidateCompleted(jobId, Read<Guid>(payload, "ModuleId"));
                break;
            case nameof(ITransferEndpoints.AnalyseTransferRefactorMap):
            {
                var moduleId = Read<Guid>(payload, "ModuleId");

                await hub.AnalyseTransferRefactorMapCompleted(
                    jobId, moduleId,
                    moduleId == SourceModuleId ? TransferRoleKind.Source : TransferRoleKind.Receiver,
                    moduleId == ConsumingModuleId ? NeedsOutputs.ToList() : [],
                    null);
                break;
            }
            case nameof(ITransferEndpoints.TransferMigrateMap):
            {
                var moduleId = Read<Guid>(payload, "ModuleId");

                // Only the source cuts a fragment; the receiver is handed one and produces none.
                var isSource = moduleId == SourceModuleId;

                await hub.TransferMigrateMapCompleted(
                    jobId, moduleId,
                    isSource ? "{\"version\":4,\"serial\":1}" : null,
                    isSource ? "map_hash: fake" : null);
                break;
            }
            case nameof(ITransferEndpoints.TransferMigrateProve):
            {
                var moduleId = Read<Guid>(payload, "ModuleId");

                // The half the other one reads from writes an outputs file; the consumer writes none.
                var outputs = moduleId == ConsumingModuleId
                    ? new Dictionary<string, string>()
                    : NeedsOutputs.ToDictionary(
                        name => $"outputs-{name}.yaml",
                        name => $"outputs:\n  {name}: fake-value\n");

                await hub.TransferMigrateProveCompleted(jobId, moduleId, 0, outputs, "clean");
                break;
            }
            case nameof(ITransferEndpoints.TransferMigrateRun):
                await hub.TransferMigrateRunCompleted(
                    jobId, Read<Guid>(payload, "ModuleId"), TransferredAddresses, gaveUp: false);
                break;
            case nameof(ITransferEndpoints.TransferMigrateVerify):
                await hub.TransferMigrateVerifyCompleted(jobId, Read<Guid>(payload, "ModuleId"));
                break;

            case nameof(ISplitEndpoints.SplitRefactorValidate):
                await hub.SplitRefactorValidateCompleted(jobId);
                break;
            case nameof(ISplitEndpoints.SplitRefactorDiff):
                await hub.SplitRefactorDiffCompleted(jobId);
                break;
            case nameof(ISplitEndpoints.SplitMigrateMap):
                await hub.SplitMigrateMapCompleted(jobId, "jobrun", [CarvedModuleName], ChangedCount);
                break;
            case nameof(ISplitEndpoints.SplitMigrateProve):
                await hub.SplitMigrateProveCompleted(jobId, ModulesProven, ModulesProven);
                break;
            case nameof(ISplitEndpoints.SplitMigrateRun):
                await hub.SplitMigrateRunCompleted(jobId);
                break;
            case nameof(ISplitEndpoints.SplitMigrateVerify):
                await hub.SplitMigrateVerifyCompleted(jobId, ModulesProven, ModulesProven);
                break;

            case nameof(ILookupAddressesEndpoints.LookupAddresses):
                // Reports every address asked about as present, which is the outcome a list has
                // when the state holds what the caller named.
                await hub.LookupAddressesCompleted(jobId,
                    (Read<List<string>>(payload, "Addresses") ?? [])
                    .Select(a => new StateAddressResult { Address = a, Outcome = "Present" })
                    .ToList());
                break;

            // Each edit and each pre-check answers on its own endpoint, so a reply can only reach
            // the job that asked for it. Every instruction is reported as succeeded.
            case nameof(IMoveEndpoints.MoveDryRun):
                await hub.MoveDryRunCompleted(jobId, Succeeded(payload));
                break;
            case nameof(IRemoveEndpoints.RemoveDryRun):
                await hub.RemoveDryRunCompleted(jobId, Succeeded(payload));
                break;
            case nameof(IImportEndpoints.ImportPreCheck):
                await hub.ImportPreCheckCompleted(jobId, Succeeded(payload));
                break;
            case nameof(IMoveEndpoints.Move):
                await hub.MoveCompleted(jobId, Succeeded(payload));
                break;
            case nameof(IImportEndpoints.Import):
                await hub.ImportCompleted(jobId, Succeeded(payload));
                break;
            case nameof(IRemoveEndpoints.Remove):
                await hub.RemoveCompleted(jobId, Succeeded(payload));
                break;

            case nameof(IDestroyEndpoints.DestroyOutput):
                await hub.DestroyOutputCompleted(jobId, new OutputSetCreateDto
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Checksum = "jobrun",
                    Outputs = []
                });
                break;
            case nameof(IApplyEndpoints.ApplyOutput):
                // An empty set rather than null: the consumer stores the set and publishes the
                // saga's completion from the same branch, so a null ends the job's progress.
                await hub.ApplyOutputCompleted(jobId, new OutputSetCreateDto
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Checksum = "jobrun",
                    Outputs = []
                });
                break;

            default:
                // An endpoint nobody answers is the end of the run, and a stall with no reason
                // given reads as the product hanging.
                Console.WriteLine($"  !! no canned reply for {endpoint}; the run will stall here");
                break;
        }
    }

    /// <summary>Every instruction the edit was asked for, reported as having worked.</summary>
    private static List<StateAddressResult> Succeeded(object payload) =>
        (Read<List<StateAddressInstruction>>(payload, "Instructions") ?? [])
        .Select(i => new StateAddressResult
        {
            Address = i.Address, Target = i.Target, Outcome = "Succeeded"
        })
        .ToList();

    /// <summary>
    /// One line per step, which is what the page's log panel reads and what raises the event that
    /// tells an open page to look again.
    /// </summary>
    private async Task SendLog(RunnerHub hub, Guid jobId, string task)
    {
        try
        {
            await hub.AddLogs([
                new LogEntryDto
                {
                    JobId = jobId,
                    ModuleId = ModuleId,
                    Timestamp = DateTimeOffset.UtcNow,
                    BatchTimeStamp = DateTimeOffset.UtcNow,
                    StackName = "jobrun",
                    NamespaceName = "jobrun",
                    Level = LogEventLevel.Information,
                    Message = $"{task}: running under jobrun",
                    TaskName = task
                }
            ]);
        }
        catch (Exception ex)
        {
            trace($"  !! log for {task} was refused: {ex.Message}");
        }
    }

    /// <summary>Keeps the current task reported until the run ends, as the runner's timer does.</summary>
    public async Task ReportPeriodically(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            if (_liveJobId == Guid.Empty) continue;

            await _reporting.WaitAsync(cancellationToken);
            try
            {
                await using var scope = services.CreateAsyncScope();
                var hub = scope.ServiceProvider.GetRequiredService<RunnerHub>();
                hub.Context = new FakeRunnerContext(connectionId, organizationId);
                await hub.ReportRunningTask(_liveJobId, _liveTask, RunnerId, instanceName);
            }
            catch (Exception ex)
            {
                trace($"  !! periodic report failed: {ex.Message}");
            }
            finally
            {
                _reporting.Release();
            }
        }
    }

    private static T? Read<T>(object payload, string name)
    {
        var value = Property(payload, name);

        // Ordinary step requests carry the Module id under Metadata; transfer-specific ones
        // carry it at the top level.
        if (value is null && name == "ModuleId" && Property(payload, "Metadata") is { } metadata)
            value = Property(metadata, "ModuleId");

        return value is null ? default : (T)value;
    }

    private static object? Property(object target, string name) =>
        target.GetType()
            .GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(target);
}
