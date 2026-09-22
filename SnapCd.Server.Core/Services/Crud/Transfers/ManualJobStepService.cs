// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Services.Crud.Transfers;

/// <summary>How a stage stands once every participant has been accounted for.</summary>
public enum StageOutcome
{
    /// <summary>At least one participant has not answered yet.</summary>
    Waiting,

    /// <summary>Every participant succeeded.</summary>
    Succeeded,

    /// <summary>A participant answered no. Not a fault: the slice ran and refused.</summary>
    Refused,

    /// <summary>A participant's step or its transport failed.</summary>
    Faulted,

    /// <summary>Every participant answered, but a result was skipped or went stale.</summary>
    Incomplete
}

/// <summary>
/// The progress record for a manual job, and the only one: a fan-in decision ("has every
/// participant finished this stage") is a query over these rows, and the same query is what the job
/// page renders, so the saga carries no counters.
/// </summary>
public class ManualJobStepService
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public ManualJobStepService(IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Records that a step was dispatched. A retry of the same task for the same Module writes a new
    /// attempt rather than overwriting, so "attempt 1 refused, attempt 2 passed" stays readable.
    /// </summary>
    public async Task<int> Dispatched(
        Guid jobId, Guid organizationId, Guid moduleId, string task,
        Guid? transferId = null, string? runnerInstanceName = null, string? inputKey = null)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var attempt = await NextAttempt(dbContext, jobId, organizationId, moduleId, task);

        dbContext.ManualModuleJobSteps.Add(new ManualModuleJobStep
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            JobId = jobId,
            TransferId = transferId,
            ModuleId = moduleId,
            Task = task,
            Attempt = attempt,
            Status = ManualJobStepStatus.Running,
            RunnerInstanceName = runnerInstanceName,
            InputKey = inputKey,
            StartedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return attempt;
    }

    /// <summary>Records a slice's reply against its latest attempt.</summary>
    public async Task Completed(
        Guid jobId, Guid organizationId, Guid moduleId, string task,
        ManualJobStepStatus status, int? exitCode = null,
        string? errorHeader = null, string? error = null)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var step = await Latest(dbContext, jobId, organizationId, moduleId, task);
        if (step == null) return;

        step.Status = status;
        step.ExitCode = exitCode;
        step.ErrorHeader = errorHeader;
        step.Error = error;
        step.EndedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Writes a step that never ran: a producer this one depends on did not succeed. Recorded rather
    /// than omitted, so the matrix shows why a cell is empty.
    /// </summary>
    public async Task Skipped(Guid jobId, Guid organizationId, Guid moduleId, string task, Guid? transferId = null)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var attempt = await NextAttempt(dbContext, jobId, organizationId, moduleId, task);

        dbContext.ManualModuleJobSteps.Add(new ManualModuleJobStep
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            JobId = jobId,
            TransferId = transferId,
            ModuleId = moduleId,
            Task = task,
            Attempt = attempt,
            Status = ManualJobStepStatus.Skipped,
            StartedAt = DateTimeOffset.UtcNow,
            EndedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Reuses an earlier green result for a slice whose inputs have not changed, citing the input
    /// key that made it reusable. This is what makes a re-run dispatch only what actually moved.
    /// </summary>
    public async Task Reused(
        Guid jobId, Guid organizationId, Guid moduleId, string task, string inputKey, Guid? transferId = null)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var attempt = await NextAttempt(dbContext, jobId, organizationId, moduleId, task);

        dbContext.ManualModuleJobSteps.Add(new ManualModuleJobStep
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            JobId = jobId,
            TransferId = transferId,
            ModuleId = moduleId,
            Task = task,
            Attempt = attempt,
            Status = ManualJobStepStatus.Succeeded,
            InputKey = inputKey,
            StartedAt = DateTimeOffset.UtcNow,
            EndedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Marks a Module's earlier green results stale, because a producer it depended on was retried.
    /// A stale result no longer counts towards the verdict.
    /// </summary>
    public async Task MarkStale(Guid jobId, Guid organizationId, Guid moduleId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        await dbContext.ManualModuleJobSteps
            .Where(s => s.JobId == jobId
                        && s.OrganizationId == organizationId
                        && s.ModuleId == moduleId
                        && s.Status == ManualJobStepStatus.Succeeded)
            .ExecuteUpdateAsync(u => u.SetProperty(s => s.Status, ManualJobStepStatus.Stale));
    }

    /// <summary>The latest attempt at each task, which is what the verdict and the matrix read.</summary>
    public async Task<IReadOnlyList<ManualModuleJobStep>> Latest(Guid jobId, Guid organizationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var steps = await dbContext.ManualModuleJobSteps.AsNoTracking()
            .Where(s => s.JobId == jobId && s.OrganizationId == organizationId)
            .ToListAsync();

        return steps
            .GroupBy(s => new { s.ModuleId, s.Task })
            .Select(g => g.OrderByDescending(s => s.Attempt).First())
            .ToList();
    }

    /// <summary>
    /// Whether every participant has finished this task, and how it went. This is the fan-in
    /// decision: the saga advances on a query over the step rows rather than on counters it keeps
    /// itself, so a replayed or duplicated completion cannot advance it twice.
    /// </summary>
    public async Task<StageOutcome> Stage(Guid jobId, Guid organizationId, string task, IReadOnlyCollection<Guid> moduleIds)
    {
        var latest = (await Latest(jobId, organizationId))
            .Where(s => s.Task == task)
            .ToDictionary(s => s.ModuleId);

        var statuses = new List<ManualJobStepStatus>();
        foreach (var moduleId in moduleIds)
        {
            if (!latest.TryGetValue(moduleId, out var step)) return StageOutcome.Waiting;
            if (step.Status is ManualJobStepStatus.Pending or ManualJobStepStatus.Running) return StageOutcome.Waiting;
            statuses.Add(step.Status);
        }

        if (statuses.Any(s => s == ManualJobStepStatus.Faulted)) return StageOutcome.Faulted;

        // A refusal is the slice answering no, which is a red verdict rather than a fault.
        if (statuses.Any(s => s == ManualJobStepStatus.Refused)) return StageOutcome.Refused;

        if (statuses.Any(s => s is ManualJobStepStatus.Skipped or ManualJobStepStatus.Stale))
            return StageOutcome.Incomplete;

        return StageOutcome.Succeeded;
    }

    private static async Task<int> NextAttempt(
        SnapCdDbContext dbContext, Guid jobId, Guid organizationId, Guid moduleId, string task)
    {
        var latest = await dbContext.ManualModuleJobSteps.AsNoTracking()
            .Where(s => s.JobId == jobId && s.OrganizationId == organizationId
                        && s.ModuleId == moduleId && s.Task == task)
            .OrderByDescending(s => s.Attempt)
            .FirstOrDefaultAsync();

        return (latest?.Attempt ?? 0) + 1;
    }

    private static async Task<ManualModuleJobStep?> Latest(
        SnapCdDbContext dbContext, Guid jobId, Guid organizationId, Guid moduleId, string task)
    {
        return await dbContext.ManualModuleJobSteps
            .Where(s => s.JobId == jobId && s.OrganizationId == organizationId
                        && s.ModuleId == moduleId && s.Task == task)
            .OrderByDescending(s => s.Attempt)
            .FirstOrDefaultAsync();
    }
}
