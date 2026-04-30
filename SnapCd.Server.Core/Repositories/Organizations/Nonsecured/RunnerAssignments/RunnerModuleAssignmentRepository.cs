using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.RunnerModuleAssignments;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerAssignments;

public class RunnerModuleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<RunnerModuleAssignmentRepositorySettings> options)
{
    public RunnerModuleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new RunnerModuleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class RunnerModuleAssignmentRepository : GenericOrganizationChildRepository<RunnerModuleAssignment, RunnerModuleAssignmentReadDto, RunnerModuleAssignmentCreatedEvent,
    RunnerModuleAssignmentUpdatedEvent, RunnerModuleAssignmentDeletedEvent, RunnerModuleAssignmentRepositorySettings>
{
    public RunnerModuleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<RunnerModuleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override RunnerModuleAssignmentReadDto MapToDto(RunnerModuleAssignment entity)
    {
        return RunnerModuleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(RunnerModuleAssignment entity)
    {
        var currentCount = await DbContext.RunnerModuleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.RunnerModuleAssignmentQuota), currentCount);
    }

    public async Task<List<RunnerModuleAssignment>> ListByRunner(Guid runnerId, Guid organizationId)
    {
        return await DbContext.RunnerModuleAssignments
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.RunnerId == runnerId)
            .ToListAsync();
    }

    public async Task<List<RunnerModuleAssignment>> ListByModule(Guid moduleId, Guid organizationId)
    {
        return await DbContext.RunnerModuleAssignments
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.ModuleId == moduleId)
            .ToListAsync();
    }
}