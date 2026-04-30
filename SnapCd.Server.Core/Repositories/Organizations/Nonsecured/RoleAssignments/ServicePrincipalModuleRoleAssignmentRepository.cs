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

public class ServicePrincipalModuleRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ServicePrincipalModuleRoleAssignmentRepositorySettings> options)
{
    public ServicePrincipalModuleRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ServicePrincipalModuleRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class ServicePrincipalModuleRoleAssignmentRepository : GenericModuleChildRepository<ServicePrincipalModuleRoleAssignment, ServicePrincipalModuleRoleAssignmentReadDto,
    ServicePrincipalModuleRoleAssignmentCreatedEvent, ServicePrincipalModuleRoleAssignmentUpdatedEvent, ServicePrincipalModuleRoleAssignmentDeletedEvent,
    ServicePrincipalModuleRoleAssignmentRepositorySettings>
{
    public ServicePrincipalModuleRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ServicePrincipalModuleRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ServicePrincipalModuleRoleAssignmentReadDto MapToDto(ServicePrincipalModuleRoleAssignment entity)
    {
        return ServicePrincipalModuleRoleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ServicePrincipalModuleRoleAssignment entity)
    {
        var currentCount = await DbContext.ServicePrincipalModuleRoleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ServicePrincipalModuleRoleAssignmentQuota), currentCount);
    }

    public async Task<List<ServicePrincipalModuleRoleAssignment>> ListByServicePrincipal(Guid servicePrincipalId, Guid organizationId)
    {
        return await DbContext.ServicePrincipalModuleRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.ServicePrincipalId == servicePrincipalId)
            .ToListAsync();
    }

    public async Task<List<ServicePrincipalModuleRoleAssignment>> ListByModule(Guid moduleId, Guid organizationId)
    {
        return await DbContext.ServicePrincipalModuleRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.ModuleId == moduleId)
            .ToListAsync();
    }

    public async Task<List<ServicePrincipalModuleRoleAssignment>> ListByRole(ModuleRole role, Guid organizationId)
    {
        return await DbContext.ServicePrincipalModuleRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RoleName == role)
            .ToListAsync();
    }
}