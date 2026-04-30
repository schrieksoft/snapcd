using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos.ModuleJobs;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ModuleJobRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModuleJobRepositorySettings> options)
{
    public ModuleJobRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleJobRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModuleJobRepository : GenericModuleChildRepository<ModuleJob, ModuleJobReadDto, ModuleJobCreatedEvent, ModuleJobUpdatedEvent, ModuleJobDeletedEvent, ModuleJobRepositorySettings>
{
    public ModuleJobRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleJobRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleJobReadDto MapToDto(ModuleJob entity)
    {
        return ModuleJobMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ModuleJob entity)
    {
        var currentCount = await DbContext.ModuleJobs
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ModuleJobQuota), currentCount);
    }

    public async Task Finalize(Guid id, Guid organizationId, ExecutionStatus status, string finalMessageType, DateTimeOffset endTime, string? definitiveRevision = null,
        ActualStateHeadline? actualStateHeadline = null, bool wrapInTransaction = false)
    {
        var job = await Get(id, organizationId);

        job.Status = status;
        job.TimestampEnd = endTime;
        job.WaitingForApproval = false;
        job.IsCurrent = false;

        if (definitiveRevision != null) job.DefinitiveRevision = definitiveRevision;

        if (actualStateHeadline != null) job.ActualStateHeadline = actualStateHeadline;

        if (wrapInTransaction)
            await Update(job);
        else
            await ExecuteUpdate(job); //E.g. if calling from MassTransit state machine, a transaction is already running
    }

    public async Task WaitingForApproval(Guid id, Guid organizationId, bool waitingForApproval, bool wrapInTransaction = false)
    {
        var job = await Get(id, organizationId);
        job.WaitingForApproval = waitingForApproval;

        if (wrapInTransaction)
            await Update(job);
        else
            await ExecuteUpdate(job); //E.g. if calling from MassTransit state machine, a transaction is already running
    }

    public async Task<string?> GetActualDefinitiveRevision(Guid moduleId, Guid organizationId)
    {
        return await DbContext.ModuleJobs
            .Where(j => j.ModuleId == moduleId &&
                        j.Status == ExecutionStatus.Completed &&
                        j.TimestampEnd != null  && j.OrganizationId == organizationId)
            .OrderByDescending(j => j.TimestampEnd)
            .Select(j => j.DefinitiveRevision)
            .FirstOrDefaultAsync();
    }

    public async Task<string?> GetLastAttemptedDefinitiveRevision(Guid moduleId, Guid organizationId)
    {
        return await DbContext.ModuleJobs
            .Where(j => j.ModuleId == moduleId && 
                        j.DefinitiveRevision != null && 
                        j.OrganizationId == organizationId)
            .OrderByDescending(j => j.TimestampStart)
            .Select(j => j.DefinitiveRevision)
            .FirstOrDefaultAsync();
    }

    public async Task<ActualStateHeadline?> GetCurrentActualStateHeadline(Guid moduleId, Guid organizationId)
    {
        return await DbContext.ModuleJobs
            .Where(j => j.ModuleId == moduleId &&
                        j.ActualStateHeadline != null &&
                        j.TimestampEnd != null && 
                        j.OrganizationId == organizationId)
            .OrderByDescending(j => j.TimestampEnd)
            .Select(j => j.ActualStateHeadline)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ActualStateHeadline>> GetRecentActualDefiniteRevisions(Guid moduleId, int revisions)
    {
        return await DbContext.ModuleJobs
            .Where(j => j.ModuleId == moduleId &&
                        j.ActualStateHeadline != null &&
                        j.TimestampEnd != null)
            .OrderByDescending(j => j.TimestampEnd)
            .Select(j => j.ActualStateHeadline!.Value)
            .Take(revisions)
            .ToListAsync();
    }

    public async Task<bool> IsFirstApply(Guid moduleId, Guid organizationId)
    {
        var currentState = await GetCurrentActualStateHeadline(moduleId,  organizationId);

        return currentState == null ||
               currentState == ActualStateHeadline.None ||
               currentState == ActualStateHeadline.Destroyed;
    }

    /// <summary>
    /// Finalizes a job that failed due to a server-side error, recording error details.
    /// </summary>
    public async Task FinalizeWithServerError(
        Guid id,
        Guid organizationId,
        string finalMessageType,
        DateTimeOffset endTime,
        ActualStateHeadline? actualStateHeadline,
        ServerSideStep? failedStep,
        string? errorHeader,
        string? errorMessage)
    {
        var job = await Get(id, organizationId);

        job.Status = ExecutionStatus.Failed;
        job.TimestampEnd = endTime;
        job.WaitingForApproval = false;
        job.IsCurrent = false;

        if (actualStateHeadline != null)
            job.ActualStateHeadline = actualStateHeadline;

        // Set server-side error fields
        if (failedStep.HasValue)
        {
            job.FailedOnServerSideStep = failedStep;
            job.ServerSideErrorHeader = Truncate(errorHeader, 255);
            job.ServerSideError = Truncate(errorMessage, 16000);
        }

        await ExecuteUpdate(job); // Called from MassTransit state machine, transaction already running
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    public async Task UpdateOutputLists(
        Guid id,
        Guid organizationId,
        string? unchangedList,
        string? createList,
        string? modifyList,
        string? destroyList,
        string? recreateList)
    {
        var job = await Get(id, organizationId);
        job.OutputsUnchangedList = Truncate(unchangedList, 4000);
        job.OutputsCreateList = Truncate(createList, 4000);
        job.OutputsModifyList = Truncate(modifyList, 4000);
        job.OutputsDestroyList = Truncate(destroyList, 4000);
        job.OutputsRecreateList = Truncate(recreateList, 4000);
        await ExecuteUpdate(job);
    }
}