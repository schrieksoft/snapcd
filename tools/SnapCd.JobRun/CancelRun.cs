// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Contracts;
using SnapCd.Contracts.Constants;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Repositories.Custom.Secured;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.JobRun;

/// <summary>
/// Cancels a move while its write is still running, which is the case the saga used to refuse.
/// The fake runner holds at the write rather than answering, so the cancel arrives while the step
/// is genuinely in flight; the kill releases it, as ProcessRegistry does for a real runner.
/// </summary>
public static class CancelRun
{
    public static async Task<int> Execute(
        IServiceProvider services,
        AsyncServiceScope scope,
        IPrincipalProvider principal,
        IDbContextFactory<SnapCdDbContext> dbFactory,
        RunOptions options)
    {
        var runner = services.GetRequiredService<FakeRunner>();
        runner.HoldAt = RunnerEndpoints.Move;

        using (var sagas = scope.ServiceProvider
                   .GetRequiredService<ModuleSagaSecuredRepositoryFactory>().Create(principal))
        {
            await sagas.SetPaused(options.ModuleId, options.OrganizationId, true, "jobrun");
        }

        var stateMigrations = scope.ServiceProvider
            .GetRequiredService<StateMigrationServiceFactory>().Create(principal);

        var job = await stateMigrations.StartMove(
            options.ModuleId, options.OrganizationId,
            options.Addresses.Select(a => new AddressInstruction
            {
                Address = a,
                Target = options.Target ?? $"{a}_moved"
            }).ToList());

        Console.WriteLine($"Started job {job.Id}");

        if (!await WaitFor(dbFactory, job.Id, j => j.WaitingForApproval == true, options.Timeout))
        {
            Console.WriteLine("Result      Unknown (never reached the approval gate)");
            return 1;
        }

        await stateMigrations.Decide(job.Id, options.ModuleId, options.OrganizationId, declined: false);
        Console.WriteLine("Approved, so the write is dispatched");

        if (!await WaitFor(dbFactory, job.Id, _ => runner.Dispatched.Contains(RunnerEndpoints.Move),
                TimeSpan.FromSeconds(60)))
        {
            Console.WriteLine("Result      Unknown (the write was never dispatched)");
            return 1;
        }

        Console.WriteLine("Write is running and holding; cancelling it");

        await stateMigrations.Cancel(
            job.Id, options.ModuleId, options.OrganizationId, CancellationType.ImmediateKill);

        var status = await Settle(dbFactory, job.Id, TimeSpan.FromSeconds(90));

        Console.WriteLine();
        Console.WriteLine($"Result      {status}");
        Console.WriteLine($"Killed      {(runner.WasKilled ? "yes, the runner was asked to stop" : "NO - the kill never reached the runner")}");
        Console.WriteLine($"Steps       {string.Join(" ", runner.Dispatched)}");

        return status == ExecutionStatus.Cancelled && runner.WasKilled ? 0 : 1;
    }

    private static async Task<bool> WaitFor(
        IDbContextFactory<SnapCdDbContext> dbFactory,
        Guid jobId,
        Func<StateMigrationJobRow, bool> reached,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(250);

            await using var db = await dbFactory.CreateDbContextAsync();
            var job = await db.StateMigrationJobs
                .Where(j => j.Id == jobId)
                .Select(j => new StateMigrationJobRow(j.Status, j.WaitingForApproval))
                .FirstOrDefaultAsync();

            if (job is null) continue;
            if (reached(job)) return true;
            if (job.Status != ExecutionStatus.Running) return false;
        }

        return false;
    }

    private static async Task<ExecutionStatus> Settle(
        IDbContextFactory<SnapCdDbContext> dbFactory, Guid jobId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);

            await using var db = await dbFactory.CreateDbContextAsync();
            var status = await db.StateMigrationJobs
                .Where(j => j.Id == jobId)
                .Select(j => j.Status)
                .FirstOrDefaultAsync();

            if (status != ExecutionStatus.Running) return status;
        }

        return ExecutionStatus.Running;
    }

    private record StateMigrationJobRow(ExecutionStatus Status, bool? WaitingForApproval);
}
