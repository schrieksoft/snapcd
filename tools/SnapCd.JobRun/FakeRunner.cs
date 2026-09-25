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
        await hub.ReportRunningTask(jobId, endpoint, RunnerId, instanceName);

        switch (endpoint)
        {
            case RunnerEndpoints.GetDefinitiveRevision:
                await hub.GetDefinitiveRevisionCompleted(jobId, "0000000000000000000000000000000000000000");
                break;
            case RunnerEndpoints.GetModule:
                await hub.GetModuleCompleted(jobId);
                break;
            case RunnerEndpoints.Init:
                await hub.InitCompleted(jobId);
                break;
            case RunnerEndpoints.Validate:
                await hub.ValidateCompleted(jobId);
                break;
            case RunnerEndpoints.Variables:
                await hub.VariablesCompleted(jobId, null);
                break;
            case RunnerEndpoints.PolicyValidate:
                await hub.PolicyValidateCompleted(jobId, PolicyOutcome);
                break;
            case RunnerEndpoints.Plan:
                await hub.PlanCompleted(jobId, new PlanCompletedData
                {
                    TotalChangedCount = ChangedCount,
                    CreateCount = ChangedCount,
                    TotalCountAfter = ChangedCount,
                    PolicyOutcome = PolicyOutcome
                });
                break;
            case RunnerEndpoints.PlanEmptyVerify:
                await hub.PlanEmptyVerifyCompleted(jobId);
                break;
            case RunnerEndpoints.ApplyFromPlan:
                await hub.ApplyFromPlanCompleted(jobId, ChangedCount);
                break;
            case RunnerEndpoints.StateListFiltered:
                // Reports every address asked about as present, which is the outcome a list has
                // when the state holds what the caller named.
                await hub.StateListFilteredCompleted(jobId,
                    (Read<List<string>>(payload, "Addresses") ?? [])
                    .Select(a => new StateAddressResult { Address = a, Outcome = "Present" })
                    .ToList());
                break;

            case RunnerEndpoints.Output:
                // An empty set rather than null: the consumer stores the set and publishes the
                // saga's completion from the same branch, so a null ends the job's progress.
                await hub.OutputCompleted(jobId, new OutputSetCreateDto
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Checksum = "jobrun",
                    Outputs = []
                });
                break;

            default:
                trace($"  !! no canned reply for {endpoint}; the run will stall here");
                break;
        }
    }

    /// <summary>Keeps the current task reported until the run ends, as the runner's timer does.</summary>
    public async Task ReportPeriodically(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            if (_liveJobId == Guid.Empty) continue;

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
        }
    }

    private static T? Read<T>(object payload, string name)
    {
        var value = payload.GetType()
            .GetProperty(name, BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(payload);

        return value is null ? default : (T)value;
    }
}
