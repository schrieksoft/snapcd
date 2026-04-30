using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ModuleInputFromSecretRepository<TEntity> : GenericModuleChildDefinitionRepository<
    TEntity,
    ModuleInputFromSecretReadDto,
    ModuleInputFromSecretCreatedEvent,
    ModuleInputFromSecretUpdatedEvent,
    ModuleInputFromSecretDeletedEvent,
    ModuleInputFromSecretRepositorySettings>
    where TEntity : ModuleInputWithType, IModuleInputFromSecret
{
    public ModuleInputFromSecretRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleInputFromSecretRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleInputFromSecretReadDto MapToDto(TEntity entity)
    {
        return ModuleInputFromSecretMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(TEntity entity)
    {
        var currentCount = await DbContext.Set<TEntity>()
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        // Determine quota name based on entity type (Param or EnvVar)
        var typeName = typeof(TEntity).Name;
        var quotaName = typeName.Contains("Param")
            ? nameof(Settings.QuotaLimits.ModuleParamFromSecretQuota)
            : nameof(Settings.QuotaLimits.ModuleEnvVarFromSecretQuota);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, quotaName, currentCount);
    }

    public override async Task<TEntity> ExecuteCreate(TEntity entity)
    {
        await ValidateSecretScope(entity);
        return await base.ExecuteCreate(entity);
    }

    public override async Task<TEntity> ExecuteUpdate(TEntity entity)
    {
        await ValidateSecretScope(entity);
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

    private async Task ValidateSecretScope(TEntity entity)
    {
        var secret = await DbContext.Secrets
            .Where(s => s.Id == entity.SecretId)
            .FirstOrDefaultAsync();

        if (secret == null) throw new InvalidSecretScopeException($"Secret with ID {entity.SecretId} not found.");

        // Get the Module's NamespaceId and Stack's StackId directly from the database to avoid tracking conflicts
        var moduleInfo = await DbContext.Modules
            .Where(m => m.Id == entity.ModuleId)
            .Select(m => new { m.NamespaceId, m.Namespace.StackId })
            .FirstOrDefaultAsync();

        if (moduleInfo == null) throw new InvalidSecretScopeException($"Module with ID {entity.ModuleId} not found.");

        // Check if the secret is a ModuleSecret
        if (secret is ModuleSecret moduleSecret)
        {
            // Ensure the module ID matches
            if (moduleSecret.ModuleId != entity.ModuleId)
                throw new InvalidSecretScopeException(
                    $"Cannot reference a ModuleSecret that belongs to a different module. " +
                    $"Secret is scoped to module {moduleSecret.ModuleId}, but input is for module {entity.ModuleId}.");
        }
        // Check if the secret is a NamespaceSecret
        else if (secret is NamespaceSecret namespaceSecret)
        {
            // Ensure the namespace ID matches
            if (namespaceSecret.NamespaceId != moduleInfo.NamespaceId)
                throw new InvalidSecretScopeException(
                    $"Cannot reference a NamespaceSecret that belongs to a different namespace. " +
                    $"Secret is scoped to namespace {namespaceSecret.NamespaceId}, but input is for module in namespace {moduleInfo.NamespaceId}.");
        }
        // Check if the secret is a StackSecret
        else if (secret is StackSecret stackSecret)
        {
            // Ensure the stack ID matches
            if (stackSecret.StackId != moduleInfo.StackId)
                throw new InvalidSecretScopeException(
                    $"Cannot reference a StackSecret that belongs to a different stack. " +
                    $"Secret is scoped to stack {stackSecret.StackId}, but input is for module in stack {moduleInfo.StackId}.");
        }
    }
}