// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Services.Logs;
using SnapCd.Contracts.Dto.Misc;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Contracts;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.RippleProbes;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.JobRun;

/// <summary>
/// The setup every job kind shares, then the job itself. The printout is the point: it is the
/// evidence that would otherwise be assembled by hand from the job row and its steps.
/// </summary>
public static class Run
{
    public static async Task<int> Execute(IServiceProvider services, RunOptions options)
    {
        // Publishing before the receive endpoints are listening drops the message silently, and
        // WaitForHealthStatus returns on timeout rather than throwing, so a run that gave up
        // waiting would look identical to one that waited successfully.
        var waitedFrom = DateTime.UtcNow;
        var health = await services.GetRequiredService<IBusControl>()
            .WaitForHealthStatus(BusHealthStatus.Healthy, TimeSpan.FromMinutes(3));

        Console.WriteLine(
            $"Bus {health} after {(DateTime.UtcNow - waitedFrom).TotalSeconds:0.0}s");

        if (health != BusHealthStatus.Healthy)
            Console.WriteLine("  !! publishing anyway; the request may be dropped");

        // Bus health is a bus-level signal, so endpoints can still be starting. Wait until they
        // stop reporting ready: a message sent to an endpoint that is not yet reading sits in its
        // queue undelivered and the job never starts.
        var readiness = services.GetRequiredService<EndpointReadiness>();
        var endpoints = await readiness.WaitUntilSettled(
            TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(2));

        Console.WriteLine(
            $"Endpoints ready: {endpoints} after {(DateTime.UtcNow - waitedFrom).TotalSeconds:0.0}s");

        // Deliberately blunt: if a long pause after every endpoint reports ready makes the job
        // start reliably, the failure is a timing window and readiness is not the whole signal.
        // If it changes nothing, the message is being lost for a reason unrelated to startup.
        var settle = TimeSpan.FromSeconds(options.SettleSeconds);
        if (settle > TimeSpan.Zero)
        {
            Console.WriteLine($"Settling for {settle.TotalSeconds:0}s before publishing");
            await Task.Delay(settle);
        }

        var dbFactory = services.GetRequiredService<IDbContextFactory<SnapCdDbContext>>();

        var runnerId = await SeedRunnerConnection(dbFactory, options);
        services.GetRequiredService<OpenConnections<RunnerHub>>().Add(options.ConnectionId);
        if (runnerId is null)
        {
            Console.Error.WriteLine(
                $"Module {options.ModuleId} has no runner configured, so nothing can be dispatched.");
            return 2;
        }

        var runner = services.GetRequiredService<FakeRunner>();
        runner.RunnerId = runnerId.Value;
        runner.ModuleId = options.ModuleId;

        // Subscribed the way the page is, so a run reports whether the page would have heard it.
        using var notifications = new NotificationWatch(
            services, options.ModuleId, line => Console.WriteLine(line));

        using var reporting = new CancellationTokenSource();
        _ = runner.ReportPeriodically(reporting.Token);

        Console.WriteLine($"Module      {options.ModuleId}");
        Console.WriteLine($"Runner      {runnerId} as '{options.RunnerInstanceName}'");
        Console.WriteLine();

        await using var scope = services.CreateAsyncScope();

        // The factories fall back to an HTTP principal when given none, which has no request to
        // read here, so every secured call is handed the run's principal explicitly.
        var principal = scope.ServiceProvider.GetRequiredService<IPrincipalProvider>();
        var jobFactory = scope.ServiceProvider.GetRequiredService<SecuredJobServiceFactory>();
        var approvalFactory = scope.ServiceProvider
            .GetRequiredService<ModuleJobApprovalSecuredRepositoryFactory>();

        // An apply and a destroy run the same saga closed over their own messages, so the run is
        // the same but for which one it asks for.
        if (options.Job is JobKind.Apply or JobKind.Destroy)
        {
            var jobId = Guid.NewGuid();

            async Task Request(Guid id)
            {
                using var jobs = jobFactory.Create(principal);
                if (options.Job == JobKind.Apply)
                    await jobs.Apply(options.ModuleId, options.OrganizationId, id);
                else
                    await jobs.Destroy(options.ModuleId, options.OrganizationId, id);
            }

            // Throwaway sends first, to test whether it is only ever a cold transport that drops
            // a message. WarmupRequested has its own consumer and its own endpoint, so warming
            // touches no queue a real message uses.
            if (options.WarmUpSends > 0)
            {
                var bus = services.GetRequiredService<IBus>();

                for (var i = 1; i <= options.WarmUpSends; i++)
                {
                    await bus.Publish(new WarmupRequested { Sequence = i },
                        sendContext => sendContext.TimeToLive = TimeSpan.FromSeconds(30));

                    Console.WriteLine($"Warm-up {i}/{options.WarmUpSends} sent");
                    if (i < options.WarmUpSends) await Task.Delay(options.WarmUpInterval);
                }

                await Task.Delay(options.WarmUpInterval);
            }

            await Request(jobId);
            Console.WriteLine($"Started job {jobId}");

            // Asking again in the same process says whether the bus is dead for the run or only
            // the first request was lost: a second job that starts means the first was dropped.
            for (var attempt = 1; attempt <= options.RetryRequests; attempt++)
            {
                var started = await WaitForJobRow(dbFactory, principal, jobId, TimeSpan.FromSeconds(20));
                if (started) break;

                jobId = Guid.NewGuid();
                await Request(jobId);
                Console.WriteLine($"  !! no job row after 20s; re-requested as {jobId} (attempt {attempt + 1})");
            }

            // A second origin, applied before watching starts, so both waves are open while the
            // consumer they share decides. Its own job is not watched: what it exists to do is
            // give that consumer a second wave to answer.
            var secondJobId = Guid.Empty;
            if (options.SecondModuleId != Guid.Empty)
            {
                secondJobId = Guid.NewGuid();
                using var jobs = jobFactory.Create(principal);
                await jobs.Apply(options.SecondModuleId, options.OrganizationId, secondJobId);
                Console.WriteLine($"Started job {secondJobId} on second origin {options.SecondModuleId}");
            }

            var status = await Watch(dbFactory, approvalFactory, principal, jobId, options,
                services.GetRequiredService<FakeRunner>(), services, secondJobId);

            await Report(dbFactory, services, jobId, options, status);
            return status == ExecutionStatus.Completed ? 0 : 1;
        }

        if (options.Job == JobKind.Cancel)
            return await CancelRun.Execute(services, scope, principal, dbFactory, options);

        if (options.Job == JobKind.Race)
            return await RaceRun.Execute(scope, principal, dbFactory, options);

        if (options.Job == JobKind.Transfer)
            return await TransferRun.Execute(services, scope, principal, dbFactory, options, notifications);

        return await StateMigrationRun.Execute(services, scope, principal, dbFactory, options, notifications);
    }

    /// <summary>
    /// Registers the fake runner against the Module's own runner pool, as a connecting runner
    /// would. Dispatch only reaches connections naming this server instance.
    /// </summary>
    private static async Task<Guid?> SeedRunnerConnection(
        IDbContextFactory<SnapCdDbContext> dbFactory, RunOptions options)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var runnerId = options.RunnerId != Guid.Empty
            ? options.RunnerId
            : await db.Modules
                .Where(m => m.Id == options.ModuleId && m.OrganizationId == options.OrganizationId)
                .Select(m => m.RunnerId)
                .FirstOrDefaultAsync();

        if (runnerId == Guid.Empty) return null;

        db.RunnerConnections.Add(new RunnerConnection
        {
            Id = Guid.NewGuid(),
            OrganizationId = options.OrganizationId,
            RunnerId = runnerId,
            InstanceName = options.RunnerInstanceName,
            SignalRConnectionId = options.ConnectionId,
            ServerInstanceId = options.ServerInstanceId
        });

        await db.SaveChangesAsync();
        return runnerId;
    }

    /// <summary>Polls for the job's row, which is the first thing the saga writes.</summary>
    private static async Task<bool> WaitForJobRow(
        IDbContextFactory<SnapCdDbContext> dbFactory, IPrincipalProvider principal, Guid jobId, TimeSpan within)
    {
        var deadline = DateTime.UtcNow + within;
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);
            await using var db = await dbFactory.CreateDbContextAsync();
            if (await db.ModuleJobs.AnyAsync(j => j.Id == jobId)) return true;
        }

        return false;
    }

    private static async Task<ExecutionStatus> Watch(
        IDbContextFactory<SnapCdDbContext> dbFactory,
        ModuleJobApprovalSecuredRepositoryFactory approvalFactory,
        IPrincipalProvider principal,
        Guid jobId,
        RunOptions options,
        FakeRunner runner,
        IServiceProvider services,
        Guid secondOriginJobId)
    {
        // Every origin whose wave this run is waiting on, which is one unless --two-origins.
        var originJobIds = secondOriginJobId == Guid.Empty
            ? new[] { jobId }
            : [jobId, secondOriginJobId];

        var deadline = DateTime.UtcNow + options.Timeout;
        var approved = false;
        ExecutionStatus? last = null;

        // A job that has not moved in a while is not slow, it is stuck: the steps themselves take
        // seconds. Waiting out the whole timeout to learn that only delays the verdict.
        var lastProgress = DateTime.UtcNow;
        var dispatched = 0;
        var reportedMissing = false;

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);

            if (runner.Dispatched.Count != dispatched)
            {
                dispatched = runner.Dispatched.Count;
                lastProgress = DateTime.UtcNow;
            }

            if (DateTime.UtcNow - lastProgress > StallAfter)
            {
                Console.WriteLine(
                    $"  nothing happened for {StallAfter.TotalSeconds:0}s after {dispatched} dispatch(es); giving up");
                return last ?? ExecutionStatus.Unknown;
            }

            await using var db = await dbFactory.CreateDbContextAsync();
            var job = await db.ModuleJobs
                .Where(j => j.Id == jobId)
                .Select(j => new { j.Status, j.WaitingForApproval })
                .FirstOrDefaultAsync();

            if (job is null)
            {
                // A row that never appears is a different failure from one that fails: the job was
                // never created, so say which of the two happened rather than reporting Unknown.
                if (!reportedMissing && DateTime.UtcNow - lastProgress > TimeSpan.FromSeconds(20))
                {
                    Console.WriteLine($"  no ModuleJobs row for {jobId} yet");
                    reportedMissing = true;
                }

                continue;
            }

            if (job.Status != last)
            {
                Console.WriteLine($"  job is {job.Status}");
                last = job.Status;
                lastProgress = DateTime.UtcNow;
            }

            if (job.Status != ExecutionStatus.Running)
            {
                await WaitForWave(dbFactory, services, originJobIds, options.OrganizationId, runner, deadline);
                return job.Status;
            }

            if (job.WaitingForApproval == true && !approved)
            {
                // The wave ends when the decision is made, so a run that wants every hop to probe
                // has to let it settle first.
                if (options.ApproveAfterWave)
                    await WaitForWave(dbFactory, services, originJobIds, options.OrganizationId, runner, deadline);

                Console.WriteLine("  approving");
                using (var approvals = approvalFactory.Create(principal))
                    await approvals.Create(new ModuleJobApproval
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = options.OrganizationId,
                        ModuleJobId = jobId,
                        DecisionDateTime = DateTime.UtcNow,
                        Declined = false,
                        PrincipalId = principal.GetSubject(options.OrganizationId),
                        PrincipalDiscriminator = principal.GetPrincipalDiscriminator(),
                        AgentId = principal.GetAgentId(),
                        Reason = "jobrun"
                    });
                approved = true;
            }
        }

        Console.WriteLine($"  timed out after {options.Timeout.TotalSeconds:0}s");
        return last ?? ExecutionStatus.Unknown;
    }

    /// <summary>
    /// Waits for the probes a job's wave started, which outlive the job itself: an apply reaches
    /// approval and completes while its consumers are still planning.
    ///
    /// A probe that is still running when the run ends is a result nobody saw, and the run reads
    /// as a pass. Reported per Module so a probe that failed or never reported says so.
    /// </summary>
    private static async Task WaitForWave(
        IDbContextFactory<SnapCdDbContext> dbFactory,
        IServiceProvider services,
        Guid[] originJobIds,
        Guid organizationId,
        FakeRunner runner,
        DateTime deadline)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var waveIds = await db.ProbeWaves
            .AsNoTracking()
            .Where(w => originJobIds.Contains(w.OriginModuleJobId))
            .Select(w => w.Id)
            .ToListAsync();

        if (waveIds.Count == 0) return;

        Console.WriteLine("  waiting for the wave");

        var lastProgress = DateTime.UtcNow;
        var dispatched = runner.Dispatched.Count;
        var settledCount = -1;

        while (DateTime.UtcNow < deadline)
        {
            await using var poll = await dbFactory.CreateDbContextAsync();

            var probes = await poll.ModuleProbeAttempts
                .AsNoTracking()
                .Where(a => waveIds.Contains(a.ProbeWaveId))
                .Join(poll.ModuleJobs, a => a.ProbeModuleJobId, j => j.Id,
                    (a, j) => new { a.ModuleId, a.SignalOutputSetId, j.Status })
                .ToListAsync();

            var running = probes.Count(p => p.Status == ExecutionStatus.Running);

            using var progress = services.GetRequiredService<ProbeWaveProgressReaderFactory>().Create();

            // Every origin has to have settled: one wave finishing says nothing about the other.
            var settlement = ProbeWaveSettlement.Settled;
            foreach (var originJobId in originJobIds)
                if (await progress.ReadForOriginJob(originJobId, organizationId) == ProbeWaveSettlement.Waiting)
                    settlement = ProbeWaveSettlement.Waiting;

            if (runner.Dispatched.Count != dispatched)
            {
                dispatched = runner.Dispatched.Count;
                lastProgress = DateTime.UtcNow;
            }

            var settled = probes.Count - running;
            if (settled != settledCount)
            {
                settledCount = settled;
                lastProgress = DateTime.UtcNow;
            }

            // A wave grows as each Module reports, so no probe running does not mean the wave is
            // over: the next hop is enrolled by the report that just landed.
            if (settlement != ProbeWaveSettlement.Waiting && running == 0) break;

            if (DateTime.UtcNow - lastProgress > StallAfter)
            {
                Console.WriteLine(
                    $"  wave stalled with {running} probe(s) still running after {dispatched} dispatch(es)");
                break;
            }

            await Task.Delay(500);
        }

        await using var final = await dbFactory.CreateDbContextAsync();

        // One row per probe rather than per Module: a Module read by two waves is probed by each,
        // in the one job, so the wave is what tells those rows apart.
        var report = await final.ModuleProbeAttempts
            .AsNoTracking()
            .Where(a => waveIds.Contains(a.ProbeWaveId))
            .Join(final.ModuleJobs, a => a.ProbeModuleJobId, j => j.Id,
                (a, j) => new
                {
                    Name = a.Module.Name,
                    a.SignalOutputSetId,
                    j.Status,
                    a.ProbeWaveId,
                    OriginName = a.ProbeWave.OriginModuleJob.Module.Name
                })
            .OrderBy(x => x.Name)
            .ThenBy(x => x.OriginName)
            .ToListAsync();

        Console.WriteLine($"Probes      {report.Count}");
        foreach (var probe in report)
            Console.WriteLine(
                $"  {probe.Name,-22} {probe.Status,-10}" +
                (probe.SignalOutputSetId == null ? "reported nothing" : "reported") +
                $"  (wave from {probe.OriginName})");
    }

    /// <summary>
    /// How long a job may go without dispatching anything or changing status before the run calls
    /// it stuck. A step answered by the fake runner takes seconds, so silence this long is failure.
    /// </summary>
    private static readonly TimeSpan StallAfter = TimeSpan.FromSeconds(75);

    private static async Task Report(
        IDbContextFactory<SnapCdDbContext> dbFactory,
        IServiceProvider services,
        Guid jobId,
        RunOptions options,
        ExecutionStatus status)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var job = await db.ModuleJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId);

        Console.WriteLine();
        Console.WriteLine($"Result      {status}");

        if (job is not null)
        {
            if (job.FailedOnServerSideStep is not null)
                Console.WriteLine($"Failed on   {job.FailedOnServerSideStep}");
            if (!string.IsNullOrWhiteSpace(job.ServerSideErrorHeader))
                Console.WriteLine($"Error       {job.ServerSideErrorHeader}");
            if (!string.IsNullOrWhiteSpace(job.ServerSideError))
                Console.WriteLine($"            {First(job.ServerSideError)}");
            if (job.PolicyOutcome is not null)
                Console.WriteLine($"Policy      {job.PolicyOutcome}");
            if (job.PlanTotalChangedCount is not null)
                Console.WriteLine($"Plan        {job.PlanTotalChangedCount} changed");
        }

        Console.WriteLine();
        var runner = services.GetRequiredService<FakeRunner>();
        Console.WriteLine($"Dispatched  {runner.Dispatched.Count} step(s)");
        foreach (var step in runner.Dispatched)
            Console.WriteLine($"  {step}");

        using var scope = services.CreateScope();
        var logs = await scope.ServiceProvider.GetRequiredService<JobLogReader<ModuleJob, ModuleJobLogChunk, ModuleJobLogEntryDto>>().GetLogEntries(jobId);
        if (logs.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Logs");
            foreach (var entry in logs.TakeLast(20))
                Console.WriteLine($"  {entry.Message?.TrimEnd()}");
        }
    }

    private static string First(string text) =>
        text.Split('\n')[0].Trim();
}
