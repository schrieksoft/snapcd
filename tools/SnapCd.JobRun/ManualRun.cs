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
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Repositories.Custom.Secured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.JobRun;

/// <summary>
/// The manual job kinds, which run against a paused Module rather than through the gatekeeper.
/// Each one records its own steps, so those are the evidence rather than the job's logs.
/// </summary>
public static class ManualRun
{
    public static async Task<int> Execute(
        IServiceProvider services,
        AsyncServiceScope scope,
        IPrincipalProvider principal,
        IDbContextFactory<SnapCdDbContext> dbFactory,
        RunOptions options)
    {
        // A manual job refuses an unpaused Module, which is the operator's own first step.
        using (var sagas = scope.ServiceProvider
                   .GetRequiredService<ModuleSagaSecuredRepositoryFactory>().Create(principal))
        {
            await sagas.SetPaused(options.ModuleId, options.OrganizationId, true, "jobrun");
        }

        Console.WriteLine("Paused the module");

        var manualJobs = scope.ServiceProvider
            .GetRequiredService<ManualJobServiceFactory>().Create(principal);

        var job = options.Job switch
        {
            JobKind.List => await manualJobs.StartStateListFiltered(
                options.ModuleId, options.OrganizationId, options.Addresses),
            JobKind.Move => await StartEdit(manualJobs, options, StateEditOperation.Move),
            JobKind.Import => await StartEdit(manualJobs, options, StateEditOperation.Import),
            JobKind.Remove => await StartEdit(manualJobs, options, StateEditOperation.Remove),
            _ => throw new NotSupportedException($"No manual run for {options.Job}.")
        };

        Console.WriteLine($"Started job {job.Id}");

        var status = await Watch(dbFactory, job.Id, options);
        await Report(dbFactory, services, job.Id, status);

        return status == ExecutionStatus.Completed ? 0 : 1;
    }

    /// <summary>
    /// A move and an import need a target per address; a remove takes none. The default stands in
    /// for one an operator would name.
    /// </summary>
    private static Task<ManualModuleJob> StartEdit(
        ManualJobService manualJobs, RunOptions options, StateEditOperation operation)
    {
        var instructions = options.Addresses
            .Select(address => new AddressInstruction
            {
                Address = address,
                Target = operation == StateEditOperation.Remove
                    ? null
                    : options.Target ?? $"{address}_moved"
            })
            .ToList();

        return manualJobs.StartStateMove(
            options.ModuleId, options.OrganizationId, operation, instructions);
    }

    private static async Task<ExecutionStatus> Watch(
        IDbContextFactory<SnapCdDbContext> dbFactory, Guid jobId, RunOptions options)
    {
        var deadline = DateTime.UtcNow + options.Timeout;
        ExecutionStatus? last = null;

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);

            await using var db = await dbFactory.CreateDbContextAsync();
            var status = await db.ManualModuleJobs
                .Where(j => j.Id == jobId)
                .Select(j => (ExecutionStatus?)j.Status)
                .FirstOrDefaultAsync();

            if (status is null) continue;

            if (status != last)
            {
                Console.WriteLine($"  job is {status}");
                last = status;
            }

            if (status != ExecutionStatus.Running)
                return status.Value;
        }

        Console.WriteLine($"  timed out after {options.Timeout.TotalSeconds:0}s");
        return last ?? ExecutionStatus.Unknown;
    }

    private static async Task Report(
        IDbContextFactory<SnapCdDbContext> dbFactory,
        IServiceProvider services,
        Guid jobId,
        ExecutionStatus status)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var job = await db.ManualModuleJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId);

        Console.WriteLine();
        Console.WriteLine($"Result      {status}");

        if (job is not null)
        {
            if (job.FailedOnServerSideStep is not null)
                Console.WriteLine($"Failed on   {job.FailedOnServerSideStep}");
            if (!string.IsNullOrWhiteSpace(job.ServerSideErrorHeader))
                Console.WriteLine($"Error       {job.ServerSideErrorHeader}");
        }

        var steps = await db.ManualModuleJobSteps.AsNoTracking()
            .Where(s => s.JobId == jobId)
            .OrderBy(s => s.StartedAt)
            .ToListAsync();

        Console.WriteLine();
        Console.WriteLine($"Steps       {steps.Count}");
        foreach (var step in steps)
        {
            Console.WriteLine($"  {step.Task,-24} {step.Status}");
            if (!string.IsNullOrWhiteSpace(step.ErrorHeader))
                Console.WriteLine($"  {"",-24} {step.ErrorHeader}");
        }

        var addresses = await db.ManualModuleJobAddresses.AsNoTracking()
            .Where(a => a.JobId == jobId)
            .OrderBy(a => a.Address)
            .ToListAsync();

        if (addresses.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Addresses   {addresses.Count}");
            foreach (var address in addresses)
                Console.WriteLine($"  {address.Address,-40} {address.Operation} {address.Outcome}");
        }

        var runner = services.GetRequiredService<FakeRunner>();
        Console.WriteLine();
        Console.WriteLine($"Dispatched  {runner.Dispatched.Count} step(s)");
        foreach (var dispatched in runner.Dispatched)
            Console.WriteLine($"  {dispatched}");
    }
}
