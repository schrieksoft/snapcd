using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ServicePrincipals;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ServicePrincipalRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ServicePrincipalRepositorySettings> options)
{
    public ServicePrincipalRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ServicePrincipalRepository(dbContext, principalProvider, bus, options);
    }
}

public class ServicePrincipalRepository : GenericOrganizationChildRepository<ServicePrincipal, ServicePrincipalReadDto, ServicePrincipalCreatedEvent, ServicePrincipalUpdatedEvent,
    ServicePrincipalDeletedEvent, ServicePrincipalRepositorySettings>
{
    public ServicePrincipalRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ServicePrincipalRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ServicePrincipalReadDto MapToDto(ServicePrincipal entity)
    {
        return ServicePrincipalMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ServicePrincipal entity)
    {
        var currentCount = await DbContext.ServicePrincipals
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ServicePrincipalQuota), currentCount);
    }

    public async Task<ServicePrincipal?> GetByClientId(string clientId, Guid organizationId)
    {
        var prefixedClientId = $"{organizationId}:{clientId}";
        return await DbContext.ServicePrincipals
            .Where(sp => sp.OrganizationId == organizationId)
            .SingleOrDefaultAsync(sp => sp.ClientId == prefixedClientId);
    }

    /// <summary>
    /// Checks if a ServicePrincipal can run a specific module via its assigned Runner.
    /// Checks runner assignments at module, namespace, and stack levels, as well as IsAssignedToAllModules flag.
    /// </summary>
    public async Task<bool> CanRunModule(Guid servicePrincipalId, Guid moduleId, Guid organizationId)
    {
        // Get the module with namespace and stack information
        var moduleInfo = await DbContext.Modules
            .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
            .Select(m => new
            {
                m.Id,
                m.NamespaceId,
                StackId = m.Namespace.StackId
            })
            .FirstOrDefaultAsync();

        if (moduleInfo == null)
            return false;

        // Check if ServicePrincipal has a Runner assigned and that Runner has access to the module
        var hasAccess = await DbContext.Runners
            .Where(r => r.ServicePrincipalId == servicePrincipalId && r.OrganizationId == organizationId)
            .AnyAsync(r =>
                // Direct module assignment
                r.RunnerModuleAssignments.Any(a => a.ModuleId == moduleId) ||
                // Namespace-level assignment
                r.RunnerNamespaceAssignments.Any(a => a.NamespaceId == moduleInfo.NamespaceId) ||
                // Stack-level assignment
                r.RunnerStackAssignments.Any(a => a.StackId == moduleInfo.StackId) ||
                // Assigned to all modules
                r.IsAssignedToAllModules);

        return hasAccess;
    }
}