// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Contracts;
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
        if (endpoint == RunnerEndpoints.Ping)
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

        switch (endpoint)
        {
            case RunnerEndpoints.ApplyGetDefinitiveRevision:
                await hub.ApplyGetDefinitiveRevisionCompleted(jobId, "0000000000000000000000000000000000000000");
                break;
            case RunnerEndpoints.DestroyGetDefinitiveRevision:
                await hub.DestroyGetDefinitiveRevisionCompleted(jobId, "0000000000000000000000000000000000000000");
                break;
            // Apply and destroy answer on their own endpoints, as every other family does.
            case RunnerEndpoints.ApplyGetModule:
                await hub.ApplyGetModuleCompleted(jobId);
                break;
            case RunnerEndpoints.ApplyInit:
                await hub.ApplyInitCompleted(jobId);
                break;
            case RunnerEndpoints.ApplyValidate:
                await hub.ApplyValidateCompleted(jobId);
                break;
            case RunnerEndpoints.ApplyVariables:
                await hub.ApplyVariablesCompleted(jobId, null);
                break;
            case RunnerEndpoints.DestroyGetModule:
                await hub.DestroyGetModuleCompleted(jobId);
                break;
            case RunnerEndpoints.DestroyInit:
                await hub.DestroyInitCompleted(jobId);
                break;
            case RunnerEndpoints.DestroyValidate:
                await hub.DestroyValidateCompleted(jobId);
                break;
            case RunnerEndpoints.DestroyVariables:
                await hub.DestroyVariablesCompleted(jobId, null);
                break;

            // The manual families run the same checkout and init, each answering on its own
            // endpoint, so each is replied to by name.
            case RunnerEndpoints.StateListFilteredGetModule:
                await hub.StateListFilteredGetModuleCompleted(jobId);
                break;
            case RunnerEndpoints.StateListFilteredInit:
                await hub.StateListFilteredInitCompleted(jobId);
                break;
            case RunnerEndpoints.MoveGetModule:
                await hub.MoveGetModuleCompleted(jobId);
                break;
            case RunnerEndpoints.MoveInit:
                await hub.MoveInitCompleted(jobId);
                break;
            case RunnerEndpoints.ImportGetModule:
                await hub.ImportGetModuleCompleted(jobId);
                break;
            case RunnerEndpoints.ImportInit:
                await hub.ImportInitCompleted(jobId);
                break;
            case RunnerEndpoints.RemoveGetModule:
                await hub.RemoveGetModuleCompleted(jobId);
                break;
            case RunnerEndpoints.RemoveInit:
                await hub.RemoveInitCompleted(jobId);
                break;
            case RunnerEndpoints.ApplyPolicyValidate:
                await hub.ApplyPolicyValidateCompleted(jobId, PolicyOutcome);
                break;
            case RunnerEndpoints.DestroyPolicyValidate:
                await hub.DestroyPolicyValidateCompleted(jobId, PolicyOutcome);
                break;
            case RunnerEndpoints.ApplyPlan:
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
            case RunnerEndpoints.SplitGetModule:
                await hub.SplitGetModuleCompleted(jobId);
                break;
            case RunnerEndpoints.SplitInit:
                await hub.SplitInitCompleted(jobId);
                break;
            case RunnerEndpoints.SplitValidate:
                await hub.SplitValidateCompleted(jobId);
                break;
            case RunnerEndpoints.SplitPlan:
                await hub.SplitPlanCompleted(jobId, new PlanCompletedData
                {
                    TotalChangedCount = ChangedCount,
                    CreateCount = ChangedCount,
                    TotalCountAfter = ChangedCount,
                    PolicyOutcome = PolicyOutcome
                });
                break;
            case RunnerEndpoints.SplitPlanEmptyVerify:
                await hub.SplitPlanEmptyVerifyCompleted(jobId);
                break;
            case RunnerEndpoints.ApplyFromPlan:
                await hub.ApplyFromPlanCompleted(jobId, ChangedCount);
                break;

            // A destroy plans what it would remove, then removes it, leaving nothing in state.
            case RunnerEndpoints.DestroyPlan:
                await hub.DestroyPlanCompleted(jobId, new PlanCompletedData
                {
                    TotalChangedCount = ChangedCount,
                    DestroyCount = ChangedCount,
                    TotalCountAfter = 0,
                    PolicyOutcome = PolicyOutcome
                });
                break;
            case RunnerEndpoints.DestroyFromPlan:
                await hub.DestroyFromPlanCompleted(jobId, 0);
                break;
            case RunnerEndpoints.TransferGetModule:
                await hub.TransferGetModuleCompleted(jobId, Read<Guid>(payload, "ModuleId"));
                break;
            case RunnerEndpoints.TransferInit:
                await hub.TransferInitCompleted(jobId, Read<Guid>(payload, "ModuleId"));
                break;
            case RunnerEndpoints.TransferValidate:
                await hub.TransferValidateCompleted(jobId, Read<Guid>(payload, "ModuleId"));
                break;
            case RunnerEndpoints.TransferPlan:
            {
                // A transfer needs a clean plan to go ahead.
                var changed = PlansClean ? 0 : ChangedCount;

                await hub.TransferPlanCompleted(jobId, Read<Guid>(payload, "ModuleId"), new PlanCompletedData
                {
                    TotalChangedCount = changed,
                    CreateCount = changed,
                    TotalCountAfter = changed,
                    PolicyOutcome = PolicyOutcome
                });
                break;
            }
            case RunnerEndpoints.TransferMigrateMap:
            {
                var moduleId = Read<Guid>(payload, "ModuleId");
                var needs = moduleId == ConsumingModuleId ? NeedsOutputs.ToList() : [];

                await hub.TransferMigrateMapCompleted(jobId, moduleId, needs);
                break;
            }
            case RunnerEndpoints.TransferMigrateProve:
                await hub.TransferMigrateProveCompleted(
                    jobId, Read<Guid>(payload, "ModuleId"), 0,
                    new Dictionary<string, string>(), "clean");
                break;
            case RunnerEndpoints.TransferMigrateRun:
                await hub.TransferMigrateRunCompleted(
                    jobId, Read<Guid>(payload, "ModuleId"), TransferredAddresses, gaveUp: false);
                break;
            case RunnerEndpoints.TransferMigrateVerify:
                await hub.TransferMigrateVerifyCompleted(jobId, Read<Guid>(payload, "ModuleId"));
                break;
            case RunnerEndpoints.TransferOutputs:
            {
                var moduleId = Read<Guid>(payload, "ModuleId");

                // The producing side publishes what the other is parked on; the consuming side
                // has nothing anyone waits for.
                var outputs = moduleId == ConsumingModuleId
                    ? []
                    : NeedsOutputs.Select(name => new OutputCreateDto
                    {
                        Name = name, Type = "string", Value = $"{name}-from-jobrun"
                    }).ToList();

                await hub.TransferOutputsCompleted(jobId, moduleId, new OutputSetCreateDto
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Checksum = $"jobrun-{moduleId:N}",
                    Outputs = outputs
                });
                break;
            }

            case RunnerEndpoints.SplitRefactorValidate:
                await hub.SplitRefactorValidateCompleted(jobId);
                break;
            case RunnerEndpoints.SplitRefactorDiff:
                await hub.SplitRefactorDiffCompleted(jobId);
                break;
            case RunnerEndpoints.SplitMigrateMap:
                await hub.SplitMigrateMapCompleted(jobId, "jobrun", [CarvedModuleName], ChangedCount);
                break;
            case RunnerEndpoints.SplitMigrateProve:
                await hub.SplitMigrateProveCompleted(jobId, ModulesProven, ModulesProven);
                break;
            case RunnerEndpoints.SplitMigrateRun:
                await hub.SplitMigrateRunCompleted(jobId);
                break;
            case RunnerEndpoints.SplitMigrateVerify:
                await hub.SplitMigrateVerifyCompleted(jobId, ModulesProven, ModulesProven);
                break;

            case RunnerEndpoints.StateListFiltered:
                // Reports every address asked about as present, which is the outcome a list has
                // when the state holds what the caller named.
                await hub.StateListFilteredCompleted(jobId,
                    (Read<List<string>>(payload, "Addresses") ?? [])
                    .Select(a => new StateAddressResult { Address = a, Outcome = "Present" })
                    .ToList());
                break;

            // Each edit and each pre-check answers on its own endpoint, so a reply can only reach
            // the job that asked for it. Every instruction is reported as succeeded.
            case RunnerEndpoints.MoveDryRun:
                await hub.MoveDryRunCompleted(jobId, Succeeded(payload));
                break;
            case RunnerEndpoints.RemoveDryRun:
                await hub.RemoveDryRunCompleted(jobId, Succeeded(payload));
                break;
            case RunnerEndpoints.ImportPreCheck:
                await hub.ImportPreCheckCompleted(jobId, Succeeded(payload));
                break;
            case RunnerEndpoints.StateMove:
                await hub.MoveCompleted(jobId, Succeeded(payload));
                break;
            case RunnerEndpoints.StateImport:
                await hub.ImportCompleted(jobId, Succeeded(payload));
                break;
            case RunnerEndpoints.StateRemove:
                await hub.RemoveCompleted(jobId, Succeeded(payload));
                break;

            case RunnerEndpoints.DestroyOutput:
                await hub.DestroyOutputCompleted(jobId, new OutputSetCreateDto
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Checksum = "jobrun",
                    Outputs = []
                });
                break;
            case RunnerEndpoints.ApplyOutput:
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
