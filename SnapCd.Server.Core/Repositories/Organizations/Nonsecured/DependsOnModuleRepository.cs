using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.DependsOnModules;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class DependsOnModuleRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<DependsOnModuleRepositorySettings> options)
{
    public DependsOnModuleRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new DependsOnModuleRepository(dbContext, principalProvider, bus, options);
    }
}

public class DependsOnModuleRepository : GenericModuleChildDefinitionRepository<DependsOnModule, DependsOnModuleReadDto, DependsOnModuleCreatedEvent, DependsOnModuleUpdatedEvent,
    DependsOnModuleDeletedEvent,
    DependsOnModuleRepositorySettings>
{
    public DependsOnModuleRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<DependsOnModuleRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }


    protected override DependsOnModuleReadDto MapToDto(DependsOnModule entity)
    {
        return DependsOnModuleMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(DependsOnModule entity)
    {
        var currentCount = await DbContext.DependsOnModules
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.DependsOnModuleQuota), currentCount);
    }

    public async Task<DependsOnModule> Get(Guid moduleId, Guid dependsOnModuleId, Guid organizationId)
    {
        var entity = await DbContext.DependsOnModules
            .SingleOrDefaultAsync(i => i.ModuleId == moduleId && i.DependsOnModuleId == dependsOnModuleId && i.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException($"DependsOnModule with ModuleId {moduleId} and DependsOnModuleId {dependsOnModuleId} not found.");

        return entity;
    }
}