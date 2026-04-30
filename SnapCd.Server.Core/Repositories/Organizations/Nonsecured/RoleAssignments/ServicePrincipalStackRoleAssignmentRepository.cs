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

public class ServicePrincipalStackRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ServicePrincipalStackRoleAssignmentRepositorySettings> options)
{
    public ServicePrincipalStackRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ServicePrincipalStackRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class ServicePrincipalStackRoleAssignmentRepository : GenericStackChildRepository<ServicePrincipalStackRoleAssignment, ServicePrincipalStackRoleAssignmentReadDto,
    ServicePrincipalStackRoleAssignmentCreatedEvent, ServicePrincipalStackRoleAssignmentUpdatedEvent, ServicePrincipalStackRoleAssignmentDeletedEvent,
    ServicePrincipalStackRoleAssignmentRepositorySettings>
{
    public ServicePrincipalStackRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ServicePrincipalStackRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ServicePrincipalStackRoleAssignmentReadDto MapToDto(ServicePrincipalStackRoleAssignment entity)
    {
        return ServicePrincipalStackRoleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ServicePrincipalStackRoleAssignment entity)
    {
        var currentCount = await DbContext.ServicePrincipalStackRoleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ServicePrincipalStackRoleAssignmentQuota), currentCount);
    }

    public async Task<List<ServicePrincipalStackRoleAssignment>> ListByServicePrincipal(Guid servicePrincipalId, Guid organizationId)
    {
        return await DbContext.ServicePrincipalStackRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.ServicePrincipalId == servicePrincipalId)
            .ToListAsync();
    }

    public async Task<List<ServicePrincipalStackRoleAssignment>> ListByStack(Guid stackId, Guid organizationId)
    {
        return await DbContext.ServicePrincipalStackRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.StackId == stackId)
            .ToListAsync();
    }

    public async Task<List<ServicePrincipalStackRoleAssignment>> ListByRole(StackRole role, Guid organizationId)
    {
        return await DbContext.ServicePrincipalStackRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RoleName == role)
            .ToListAsync();
    }
}