// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Repositories.Custom.Secured;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.JobRun;

/// <summary>
/// A transfer, which is two jobs rather than one: a Module and its counterparty, each moving its
/// own share of the state. Modelled on the bootstrap-transfer sample, where the networking root
/// produces an output the app root's plan reads, so one side genuinely waits for the other.
/// </summary>
public static class TransferRun
{
    public static async Task<int> Execute(
        IServiceProvider services,
        AsyncServiceScope scope,
        IPrincipalProvider principal,
        IDbContextFactory<SnapCdDbContext> dbFactory,
        RunOptions options)
    {
        var counterpartyId = options.CounterpartyModuleId;

        // Both sides refuse an unpaused Module, as an operator would find.
        using (var sagas = scope.ServiceProvider
                   .GetRequiredService<ModuleSagaSecuredRepositoryFactory>().Create(principal))
        {
            await sagas.SetPaused(options.ModuleId, options.OrganizationId, true, "jobrun");
            await sagas.SetPaused(counterpartyId, options.OrganizationId, true, "jobrun");
        }

        // The sample's app root reads the networking root's output, and app is the side that
        // starts the transfer, so the starting Module is the one that waits.
        var runner = services.GetRequiredService<FakeRunner>();
        runner.ConsumingModuleId = options.ModuleId;
        runner.NeedsOutputs = options.NeedsOutputs;
        runner.PlansClean = true;

        Console.WriteLine($"Counterparty {counterpartyId}");
        Console.WriteLine($"Cross-over   {options.ModuleId} reads {string.Join(", ", options.NeedsOutputs)}");
        Console.WriteLine("Paused both modules");

        var transfers = scope.ServiceProvider
            .GetRequiredService<TransferServiceFactory>().Create(principal);

        var transfer = await transfers.Open(
            options.ModuleId, counterpartyId, options.OrganizationId,
            moduleRef: options.ModuleRef, startImmediately: false);

        Console.WriteLine($"Opened transfer {transfer.Id}");
        Console.WriteLine("  the starting side is now waiting to be agreed to");

        // The wait is the point, so it is observed rather than skipped.
        await AwaitConsentWait(dbFactory, transfer.Id, options);

        await transfers.Decide(
            transfer.Id, counterpartyId, options.OrganizationId,
            granted: true, moduleRef: options.CounterpartyRef);

        Console.WriteLine("  the counterparty agreed, so both sides go ahead");

        var manualJobs = scope.ServiceProvider
            .GetRequiredService<SnapCd.Server.Core.Services.Crud.Jobs.ManualJobServiceFactory>()
            .Create(principal);

        var status = await Watch(dbFactory, manualJobs, transfer.Id, options);
        await Report(dbFactory, services, transfer.Id, options, status);

        return status ? 0 : 1;
    }

    /// <summary>
    /// Waits until the starting side says it is waiting for consent, so a run that never gets
    /// there is visible rather than silently answered.
    /// </summary>
    private static async Task AwaitConsentWait(
        IDbContextFactory<SnapCdDbContext> dbFactory, Guid transferId, RunOptions options)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);

            await using var db = await dbFactory.CreateDbContextAsync();
            var waiting = await db.ManualModuleJobs
                .Where(j => j.TransferId == transferId && j.WaitingForConsent == true)
                .AnyAsync();

            if (waiting)
            {
                Console.WriteLine("  confirmed: it is waiting for consent");
                return;
            }
        }

        Console.WriteLine("  !! it never reported waiting for consent");
    }

    /// <summary>Both sides have to end before the transfer has.</summary>
    private static async Task<bool> Watch(
        IDbContextFactory<SnapCdDbContext> dbFactory,
        SnapCd.Server.Core.Services.Crud.Jobs.ManualJobService manualJobs,
        Guid transferId,
        RunOptions options)
    {
        var deadline = DateTime.UtcNow + options.Timeout;
        var seenWaitingForOutputs = false;
        var approved = new HashSet<Guid>();
        var reported = new Dictionary<Guid, ExecutionStatus>();

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);

            await using var db = await dbFactory.CreateDbContextAsync();

            var jobs = await db.ManualModuleJobs
                .Where(j => j.TransferId == transferId)
                .Select(j => new { j.Id, j.ModuleId, j.Status, j.WaitingForApproval })
                .ToListAsync();

            // Each side is approved on its own, as an operator would have to.
            foreach (var job in jobs.Where(j =>
                         j.WaitingForApproval == true && !approved.Contains(j.Id)))
            {
                Console.WriteLine($"  approving Module {job.ModuleId}");
                await manualJobs.Decide(
                    job.Id, job.ModuleId, options.OrganizationId, declined: false);
                approved.Add(job.Id);
            }

            foreach (var job in jobs)
            {
                if (reported.TryGetValue(job.Id, out var was) && was == job.Status) continue;

                reported[job.Id] = job.Status;
                Console.WriteLine($"  Module {job.ModuleId}: {job.Status}");
            }

            if (!seenWaitingForOutputs)
            {
                var parked = await db.Set<SnapCd.Server.Core.Entities.Sagas.TransferMigrateSaga>()
                    .AsNoTracking()
                    .Where(s => s.CurrentState == "WaitingForOutputs")
                    .Select(s => s.ModuleId)
                    .FirstOrDefaultAsync();

                if (parked != Guid.Empty)
                {
                    seenWaitingForOutputs = true;
                    Console.WriteLine($"  Module {parked} is waiting on the other side's outputs");
                }
            }

            if (jobs.Count == 2 && jobs.All(j => j.Status != ExecutionStatus.Running))
            {
                if (!seenWaitingForOutputs)
                    Console.WriteLine("  !! neither side ever waited on outputs; the cross-over was not exercised");

                return jobs.All(j => j.Status == ExecutionStatus.Completed);
            }
        }

        Console.WriteLine($"  timed out after {options.Timeout.TotalSeconds:0}s");
        return false;
    }

    private static async Task Report(
        IDbContextFactory<SnapCdDbContext> dbFactory,
        IServiceProvider services,
        Guid transferId,
        RunOptions options,
        bool completed)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var transfer = await db.Transfers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == transferId);
        var jobs = await db.ManualModuleJobs.AsNoTracking()
            .Where(j => j.TransferId == transferId)
            .OrderBy(j => j.TimestampStart)
            .ToListAsync();

        Console.WriteLine();
        Console.WriteLine($"Result      {(completed ? "Completed" : "Did not complete")}");
        Console.WriteLine($"Consent     {transfer?.ConsentStatus}");

        foreach (var job in jobs)
        {
            Console.WriteLine();
            Console.WriteLine($"Module {job.ModuleId}  {job.Status}");

            if (!string.IsNullOrWhiteSpace(job.ServerSideErrorHeader))
                Console.WriteLine($"  error: {job.ServerSideErrorHeader}");

            var steps = await db.ManualModuleJobSteps.AsNoTracking()
                .Where(s => s.JobId == job.Id)
                .OrderBy(s => s.StartedAt)
                .ToListAsync();

            foreach (var step in steps)
                Console.WriteLine($"  {step.Task,-24} {step.Status}");

            var addresses = await db.ManualModuleJobAddresses.AsNoTracking()
                .Where(a => a.JobId == job.Id)
                .OrderBy(a => a.Address)
                .ToListAsync();

            foreach (var address in addresses)
                Console.WriteLine($"  {address.Address,-40} {address.Operation} {address.Outcome}");
        }

        var runner = services.GetRequiredService<FakeRunner>();
        Console.WriteLine();
        Console.WriteLine($"Dispatched  {runner.Dispatched.Count} step(s)");
    }
}
