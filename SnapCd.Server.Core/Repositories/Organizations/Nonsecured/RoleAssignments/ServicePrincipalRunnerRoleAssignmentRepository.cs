using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments;

public class ServicePrincipalRunnerRoleAssignmentRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ServicePrincipalRunnerRoleAssignmentRepositorySettings> options)
{
    public ServicePrincipalRunnerRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ServicePrincipalRunnerRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class ServicePrincipalRunnerRoleAssignmentRepository : GenericOrganizationChildRepository<ServicePrincipalRunnerRoleAssignment, ServicePrincipalRunnerRoleAssignmentReadDto,
    ServicePrincipalRunnerRoleAssignmentCreatedEvent, ServicePrincipalRunnerRoleAssignmentUpdatedEvent, ServicePrincipalRunnerRoleAssignmentDeletedEvent,
    ServicePrincipalRunnerRoleAssignmentRepositorySettings>
{
    public ServicePrincipalRunnerRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ServicePrincipalRunnerRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ServicePrincipalRunnerRoleAssignmentReadDto MapToDto(ServicePrincipalRunnerRoleAssignment entity)
    {
        return ServicePrincipalRunnerRoleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ServicePrincipalRunnerRoleAssignment entity)
    {
        var currentCount = await DbContext.ServicePrincipalRunnerRoleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ServicePrincipalRunnerRoleAssignmentQuota), currentCount);
    }

    public async Task<List<ServicePrincipalRunnerRoleAssignment>> ListByServicePrincipal(Guid servicePrincipalId, Guid organizationId)
    {
        return await DbContext.ServicePrincipalRunnerRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.ServicePrincipalId == servicePrincipalId)
            .ToListAsync();
    }

    public async Task<List<ServicePrincipalRunnerRoleAssignment>> ListByRunner(Guid runnerId, Guid organizationId)
    {
        return await DbContext.ServicePrincipalRunnerRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RunnerId == runnerId)
            .ToListAsync();
    }

    public async Task<List<ServicePrincipalRunnerRoleAssignment>> ListByRole(RunnerRole role, Guid organizationId)
    {
        return await DbContext.ServicePrincipalRunnerRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RoleName == role)
            .ToListAsync();
    }
}