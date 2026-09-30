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
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Repositories.Custom.Secured;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.JobRun;

/// <summary>
/// Raises an approval in a state that rejects it, which is what a reply beating the saga's own
/// transition does. The saga binds ApprovedEvent only during MigrateStatePending, so the same
/// event raised during WaitingForApproval throws UnhandledEventException - the exception the
/// endpoint's retry filter exists to absorb.
///
/// An ordinary run cannot produce this: it polls for WaitingForApproval and decides once it is
/// already set, by which time the transition has long landed.
/// </summary>
public static class RaceRun
{
    public static async Task<int> Execute(
        AsyncServiceScope scope,
        IPrincipalProvider principal,
        IDbContextFactory<SnapCdDbContext> dbFactory,
        RunOptions options)
    {
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

        var reached = await WaitForWaitingForApproval(dbFactory, job.Id, options.Timeout);
        if (!reached)
        {
            Console.WriteLine("Result      Unknown (job never reached WaitingForApproval)");
            return 1;
        }

        Console.WriteLine("Job is WaitingForApproval; raising MoveApproved, which that state rejects");

        var bus = scope.ServiceProvider.GetRequiredService<IBus>();
        await bus.Publish(new MoveApproved
        {
            ModuleJobId = job.Id,
            OrganizationId = options.OrganizationId
        });

        // The rejection is only worth retrying because the saga is about to leave the state that
        // refuses it. Approving now is what makes it leave, so the retries land in the accepting
        // state - a saga parked in WaitingForApproval forever would exhaust them instead, which
        // says nothing about the race this guards.
        if (!options.RaceWithoutApproval)
        {
            await stateMigrations.Decide(job.Id, options.ModuleId, options.OrganizationId, declined: false);
            Console.WriteLine("Approved, so the retries land after the transition");
        }
        else
        {
            Console.WriteLine("Left unapproved, so every retry is refused");
        }

        // The retry re-runs the consumer in process, so the outcome shows up within a few seconds.
        // Long enough to cover all five attempts at a second apart.
        await Task.Delay(TimeSpan.FromSeconds(15));

        var faulted = await FaultedCount(dbFactory, job.Id);

        Console.WriteLine();
        Console.WriteLine("Read the log above for the verdict:");
        Console.WriteLine("  R-RETRY naming MoveApproved, no R-FAULT for that id -> the retry rescued it");
        Console.WriteLine("  R-RETRY then R-FAULT for the same id                -> attempts exhausted");
        Console.WriteLine("  neither                                             -> no retry was reached");
        Console.WriteLine();
        Console.WriteLine($"Job status  {faulted}");

        return 0;
    }

    private static async Task<ExecutionStatus> FaultedCount(
        IDbContextFactory<SnapCdDbContext> dbFactory, Guid jobId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var job = await db.StateMigrationJobs.AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId);
        return job?.Status ?? ExecutionStatus.Unknown;
    }

    private static async Task<bool> WaitForWaitingForApproval(
        IDbContextFactory<SnapCdDbContext> dbFactory, Guid jobId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(250);

            await using var db = await dbFactory.CreateDbContextAsync();
            var job = await db.StateMigrationJobs
                .Where(j => j.Id == jobId)
                .Select(j => new { j.Status, j.WaitingForApproval })
                .FirstOrDefaultAsync();

            if (job is null) continue;
            if (job.WaitingForApproval == true) return true;
            if (job.Status != ExecutionStatus.Running) return false;
        }

        return false;
    }
}
