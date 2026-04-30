using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class RunnerConnectionJobRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<RunnerConnectionJobRepositorySettings> options)
{
    public RunnerConnectionJobRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new RunnerConnectionJobRepository(dbContext, principalProvider, bus, options);
    }
}

public class RunnerConnectionJobRepository : GenericOrganizationChildRepository<RunnerConnectionJob,
    RunnerConnectionJobReadDto,
    RunnerConnectionJobCreatedEvent, RunnerConnectionJobUpdatedEvent, RunnerConnectionJobDeletedEvent,
    RunnerConnectionJobRepositorySettings>
{
    public RunnerConnectionJobRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<RunnerConnectionJobRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override RunnerConnectionJobReadDto MapToDto(RunnerConnectionJob entity)
    {
        return RunnerConnectionJobMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(RunnerConnectionJob entity)
    {
        var currentCount = await DbContext.RunnerConnectionJobs
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.RunnerConnectionJobQuota), currentCount);
    }

    public async Task CreateOrUpdate(Guid organizationId, Guid moduleJobId, string taskName, Guid runnerId, string? runnerInstanceName)
    {
        // Get the active runner connection
        var runnerConnection =  await DbContext.RunnerConnections
            .Where(rc => rc.OrganizationId == organizationId &&
                         rc.RunnerId == runnerId &&
                         rc.InstanceName == runnerInstanceName)
            .Select( rc => new {rc.Id})
            .FirstOrDefaultAsync();

        if (runnerConnection == null)
        {
            throw new InvalidOperationException(
                $"No active runner connection found for runner {runnerId} with instance name '{runnerInstanceName}' in organization {organizationId}");
        }
        
        var existingRunnerConnectionJob =  await DbContext.RunnerConnectionJobs
            .Where(rcj => rcj.OrganizationId == organizationId &&
                          rcj.ModuleJobId == moduleJobId &&
                          rcj.RunnerConnection.RunnerId == runnerId &&
                          rcj.RunnerConnection.InstanceName == runnerInstanceName)
            .FirstOrDefaultAsync();

        if (existingRunnerConnectionJob != null)
        {
            // Update existing record
            existingRunnerConnectionJob.RunnerConnectionId = runnerConnection.Id;
            existingRunnerConnectionJob.TaskName = taskName;

            await Update(existingRunnerConnectionJob);
        }
        else
        {
            // Create new record
            var newJob = new RunnerConnectionJob
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                RunnerConnectionId = runnerConnection.Id,
                ModuleJobId = moduleJobId,
                TaskName = taskName
            };

            await Create(newJob);
        }
    }
}
