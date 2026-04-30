using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.RunnerNamespaceAssignments;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerAssignments;

public class RunnerNamespaceAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<RunnerNamespaceAssignmentRepositorySettings> options)
{
    public RunnerNamespaceAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new RunnerNamespaceAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class RunnerNamespaceAssignmentRepository : GenericOrganizationChildRepository<RunnerNamespaceAssignment, RunnerNamespaceAssignmentReadDto, RunnerNamespaceAssignmentCreatedEvent,
    RunnerNamespaceAssignmentUpdatedEvent, RunnerNamespaceAssignmentDeletedEvent, RunnerNamespaceAssignmentRepositorySettings>
{
    public RunnerNamespaceAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<RunnerNamespaceAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override RunnerNamespaceAssignmentReadDto MapToDto(RunnerNamespaceAssignment entity)
    {
        return RunnerNamespaceAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(RunnerNamespaceAssignment entity)
    {
        var currentCount = await DbContext.RunnerNamespaceAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.RunnerNamespaceAssignmentQuota), currentCount);
    }

    public async Task<List<RunnerNamespaceAssignment>> ListByRunner(Guid runnerId, Guid organizationId)
    {
        return await DbContext.RunnerNamespaceAssignments
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.RunnerId == runnerId)
            .ToListAsync();
    }

    public async Task<List<RunnerNamespaceAssignment>> ListByNamespace(Guid namespaceId, Guid organizationId)
    {
        return await DbContext.RunnerNamespaceAssignments
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.NamespaceId == namespaceId)
            .ToListAsync();
    }
}