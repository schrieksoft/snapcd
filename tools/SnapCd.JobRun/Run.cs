// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
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
        // Publishing before the receive endpoints are listening drops the message silently.
        await services.GetRequiredService<IBusControl>()
            .WaitForHealthStatus(BusHealthStatus.Healthy, TimeSpan.FromSeconds(30));

        var dbFactory = services.GetRequiredService<IDbContextFactory<SnapCdDbContext>>();

        var runnerId = await SeedRunnerConnection(dbFactory, options);
        if (runnerId is null)
        {
            Console.Error.WriteLine(
                $"Module {options.ModuleId} has no runner configured, so nothing can be dispatched.");
            return 2;
        }

        var runner = services.GetRequiredService<FakeRunner>();
        runner.RunnerId = runnerId.Value;

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

        if (options.Job == JobKind.Apply)
        {
            var jobId = Guid.NewGuid();
            using (var jobs = jobFactory.Create(principal))
                await jobs.Apply(options.ModuleId, options.OrganizationId, jobId);

            Console.WriteLine($"Started job {jobId}");

            var status = await Watch(dbFactory, approvalFactory, principal, jobId, options);

            await Report(dbFactory, services, jobId, options, status);
            return status == ExecutionStatus.Completed ? 0 : 1;
        }

        return await ManualRun.Execute(services, scope, principal, dbFactory, options);
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

    private static async Task<ExecutionStatus> Watch(
        IDbContextFactory<SnapCdDbContext> dbFactory,
        ModuleJobApprovalSecuredRepositoryFactory approvalFactory,
        IPrincipalProvider principal,
        Guid jobId,
        RunOptions options)
    {
        var deadline = DateTime.UtcNow + options.Timeout;
        var approved = false;
        ExecutionStatus? last = null;

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);

            await using var db = await dbFactory.CreateDbContextAsync();
            var job = await db.ModuleJobs
                .Where(j => j.Id == jobId)
                .Select(j => new { j.Status, j.WaitingForApproval })
                .FirstOrDefaultAsync();

            if (job is null) continue;

            if (job.Status != last)
            {
                Console.WriteLine($"  job is {job.Status}");
                last = job.Status;
            }

            if (job.Status != ExecutionStatus.Running)
                return job.Status;

            if (job.WaitingForApproval == true && !approved)
            {
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

        var runner = services.GetRequiredService<FakeRunner>();
        Console.WriteLine();
        Console.WriteLine($"Dispatched  {runner.Dispatched.Count} step(s)");
        foreach (var step in runner.Dispatched)
            Console.WriteLine($"  {step}");

        if (!string.IsNullOrWhiteSpace(job?.Logs))
        {
            Console.WriteLine();
            Console.WriteLine("Logs");
            foreach (var line in job.Logs.Split('\n').TakeLast(20))
                Console.WriteLine($"  {line.TrimEnd()}");
        }
    }

    private static string First(string text) =>
        text.Split('\n')[0].Trim();
}
