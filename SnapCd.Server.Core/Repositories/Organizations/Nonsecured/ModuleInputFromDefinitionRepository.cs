// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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

public class ModuleInputFromDefinitionRepository<TEntity> : GenericModuleChildDefinitionRepository<
    TEntity,
    ModuleInputFromDefinitionReadDto,
    ModuleInputFromDefinitionCreatedEvent,
    ModuleInputFromDefinitionUpdatedEvent,
    ModuleInputFromDefinitionDeletedEvent,
    ModuleInputFromDefinitionRepositorySettings>
    where TEntity : ModuleInput, IModuleInputFromDefinition
{
    public ModuleInputFromDefinitionRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleInputFromDefinitionRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleInputFromDefinitionReadDto MapToDto(TEntity entity)
    {
        return ModuleInputFromDefinitionMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(TEntity entity)
    {
        var currentCount = await DbContext.Set<TEntity>()
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        // Determine quota name based on entity type (Param or EnvVar)
        var typeName = typeof(TEntity).Name;
        var quotaName = typeName.Contains("Param")
            ? nameof(Settings.QuotaLimits.ModuleParamFromDefinitionQuota)
            : nameof(Settings.QuotaLimits.ModuleEnvVarFromDefinitionQuota);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, quotaName, currentCount);
    }

    public async Task<TEntity> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await DbContext.Set<TEntity>()
            .SingleOrDefaultAsync(i => i.Name == name && i.ModuleId == moduleId && i.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException($"{typeof(TEntity).Name} with name {name} not found.");

        return entity;
    }
}