using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ModuleInputFromNamespaceRepository<TEntity> : GenericModuleChildDefinitionRepository<
    TEntity,
    ModuleInputFromNamespaceReadDto,
    ModuleInputFromNamespaceCreatedEvent,
    ModuleInputFromNamespaceUpdatedEvent,
    ModuleInputFromNamespaceDeletedEvent,
    ModuleInputFromNamespaceRepositorySettings>
    where TEntity : ModuleInput, IModuleInputFromNamespace
{
    public ModuleInputFromNamespaceRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleInputFromNamespaceRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleInputFromNamespaceReadDto MapToDto(TEntity entity)
    {
        return ModuleInputFromNamespaceMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(TEntity entity)
    {
        var currentCount = await DbContext.Set<TEntity>()
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        // Determine quota name based on entity type (Param or EnvVar)
        var typeName = typeof(TEntity).Name;
        var quotaName = typeName.Contains("Param")
            ? nameof(Settings.QuotaLimits.ModuleParamFromNamespaceQuota)
            : nameof(Settings.QuotaLimits.ModuleEnvVarFromNamespaceQuota);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, quotaName, currentCount);
    }

    public override async Task<TEntity> ExecuteCreate(TEntity entity)
    {
        await ValidateNamespaceInputScope(entity);
        return await base.ExecuteCreate(entity);
    }

    public override async Task<TEntity> ExecuteUpdate(TEntity entity)
    {
        await ValidateNamespaceInputScope(entity);
        return await base.ExecuteUpdate(entity);
    }

    public async Task<TEntity> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await DbContext.Set<TEntity>()
            .SingleOrDefaultAsync(i => i.Name == name && i.ModuleId == moduleId && i.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException($"{typeof(TEntity).Name} with name {name} not found.");

        return entity;
    }

    private async Task ValidateNamespaceInputScope(TEntity entity)
    {
        // Get the NamespaceInput that this ModuleInputFromNamespace is referencing
        var namespaceInput = await DbContext.NamespaceInputs
            .Where(ni => ni.Id == entity.NamespaceInputId)
            .FirstOrDefaultAsync();

        if (namespaceInput == null) throw new InvalidNamespaceRefereceException($"NamespaceInput with ID {entity.NamespaceInputId} not found.");

        // Get the Module's NamespaceId directly from the database to avoid tracking conflicts
        var moduleNamespaceId = await DbContext.Modules
            .Where(m => m.Id == entity.ModuleId)
            .Select(m => m.NamespaceId)
            .FirstOrDefaultAsync();

        if (moduleNamespaceId == Guid.Empty) throw new InvalidNamespaceRefereceException($"Module with ID {entity.ModuleId} not found.");

        // Validate that the NamespaceInput belongs to the same namespace as the Module
        if (namespaceInput.NamespaceId != moduleNamespaceId)
            throw new InvalidNamespaceRefereceException(
                $"Cannot reference a NamespaceInput from a different namespace. " +
                $"NamespaceInput belongs to namespace {namespaceInput.NamespaceId}, " +
                $"but ModuleInputFromNamespace is for module in namespace {moduleNamespaceId}.");
    }
}