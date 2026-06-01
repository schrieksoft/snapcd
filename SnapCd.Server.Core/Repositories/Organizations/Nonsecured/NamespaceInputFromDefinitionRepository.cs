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
using SnapCd.Contracts.Dto.NamespaceInputs;
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

public class NamespaceInputFromDefinitionRepository<TEntity> : GenericNamespaceChildDefinitionRepository<
    TEntity,
    NamespaceInputFromDefinitionReadDto,
    NamespaceInputFromDefinitionCreatedEvent,
    NamespaceInputFromDefinitionUpdatedEvent,
    NamespaceInputFromDefinitionDeletedEvent,
    NamespaceInputFromDefinitionRepositorySettings>
    where TEntity : NamespaceInput, INamespaceInputFromDefinition
{
    public NamespaceInputFromDefinitionRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<NamespaceInputFromDefinitionRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override NamespaceInputFromDefinitionReadDto MapToDto(TEntity entity)
    {
        return NamespaceInputFromDefinitionMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(TEntity entity)
    {
        var currentCount = await DbContext.Set<TEntity>()
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        // Determine quota name based on entity type (Param or EnvVar)
        var typeName = typeof(TEntity).Name;
        var quotaName = typeName.Contains("Param")
            ? nameof(Settings.QuotaLimits.NamespaceParamFromDefinitionQuota)
            : nameof(Settings.QuotaLimits.NamespaceEnvVarFromDefinitionQuota);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, quotaName, currentCount);
    }

    public async Task<TEntity> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await DbContext.Set<TEntity>()
            .Where(i => i.OrganizationId == organizationId)
            .SingleOrDefaultAsync(i => i.Name == name && i.NamespaceId == namespaceId);

        if (entity == null)
            throw new EntityNotFoundException($"{typeof(TEntity).Name} with name {name} not found.");

        return entity;
    }
}