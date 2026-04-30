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

public class ModuleInputFromOutputSetRepository<TEntity> : GenericModuleChildDefinitionRepository<
    TEntity,
    ModuleInputFromOutputSetReadDto,
    ModuleInputFromOutputSetCreatedEvent,
    ModuleInputFromOutputSetUpdatedEvent,
    ModuleInputFromOutputSetDeletedEvent,
    ModuleInputFromOutputSetRepositorySettings>
    where TEntity : ModuleInput, IModuleInputFromOutputSet
{
    public ModuleInputFromOutputSetRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleInputFromOutputSetRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleInputFromOutputSetReadDto MapToDto(TEntity entity)
    {
        return ModuleInputFromOutputSetMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(TEntity entity)
    {
        var currentCount = await DbContext.Set<TEntity>()
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        // Only Param is supported for FromOutputSet (EnvVar from OutputSet was removed)
        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ModuleParamFromOutputSetQuota), currentCount);
    }

    public override async Task<TEntity> ExecuteCreate(TEntity entity)
    {
        ValidateNotSelfReferencing(entity);
        await ValidateOutputModuleStackScope(entity);
        return await base.ExecuteCreate(entity);
    }

    public override async Task<TEntity> ExecuteUpdate(TEntity entity)
    {
        ValidateNotSelfReferencing(entity);
        await ValidateOutputModuleStackScope(entity);
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

    private void ValidateNotSelfReferencing(TEntity entity)
    {
        if (entity.ModuleId == entity.OutputModuleId) throw new ArgumentException("A module cannot reference its own outputs as inputs");
    }

    private async Task ValidateOutputModuleStackScope(TEntity entity)
    {
        // 1. Check if the output module exists
        var outputModule = await DbContext.Modules
            .Include(m => m.Namespace)
            .FirstOrDefaultAsync(m => m.Id == entity.OutputModuleId);

        if (outputModule == null)
            throw new InvalidStackReferenceException(
                $"Output module with ID {entity.OutputModuleId} not found.");

        // 2. Get the current module's stack ID
        var currentModuleStackId = await DbContext.Modules
            .Where(m => m.Id == entity.ModuleId)
            .Select(m => m.Namespace.StackId)
            .FirstOrDefaultAsync();

        if (currentModuleStackId == Guid.Empty)
            throw new InvalidStackReferenceException(
                $"Module with ID {entity.ModuleId} not found.");

        // 3. Compare stack IDs
        if (outputModule.Namespace.StackId != currentModuleStackId)
            throw new InvalidStackReferenceException(
                $"Cannot reference output from module in different stack. " +
                $"Output module belongs to stack {outputModule.Namespace.StackId}, " +
                $"but current module belongs to stack {currentModuleStackId}.");
    }
}