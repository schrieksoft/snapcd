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
using SnapCd.Contracts.Dto.NamespaceAdditionalTriggerPaths;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Utils;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class NamespaceAdditionalTriggerPathRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<NamespaceAdditionalTriggerPathRepositorySettings> options)
{
    public NamespaceAdditionalTriggerPathRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceAdditionalTriggerPathRepository(dbContext, principalProvider, bus, options);
    }
}

public class NamespaceAdditionalTriggerPathRepository : GenericNamespaceChildDefinitionRepository<
    NamespaceAdditionalTriggerPath,
    NamespaceAdditionalTriggerPathReadDto,
    NamespaceAdditionalTriggerPathCreatedEvent,
    NamespaceAdditionalTriggerPathUpdatedEvent,
    NamespaceAdditionalTriggerPathDeletedEvent,
    NamespaceAdditionalTriggerPathRepositorySettings>
{
    public NamespaceAdditionalTriggerPathRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<NamespaceAdditionalTriggerPathRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    public override async Task<NamespaceAdditionalTriggerPath> ExecuteCreate(NamespaceAdditionalTriggerPath entity)
    {
        TriggerPathValidator.EnsureValid(entity.Path);
        return await base.ExecuteCreate(entity);
    }

    public override async Task<NamespaceAdditionalTriggerPath> ExecuteUpdate(NamespaceAdditionalTriggerPath entity)
    {
        TriggerPathValidator.EnsureValid(entity.Path);
        return await base.ExecuteUpdate(entity);
    }

    protected override NamespaceAdditionalTriggerPathReadDto MapToDto(NamespaceAdditionalTriggerPath entity)
    {
        return NamespaceAdditionalTriggerPathMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(NamespaceAdditionalTriggerPath entity)
    {
        var currentCount = await DbContext.NamespaceAdditionalTriggerPaths
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.NamespaceAdditionalTriggerPathQuota), currentCount);
    }

    public async Task<NamespaceAdditionalTriggerPath> Get(Guid namespaceId, string path, Guid organizationId)
    {
        var entity = await DbContext.NamespaceAdditionalTriggerPaths
            .Where(i => i.OrganizationId == organizationId)
            .SingleOrDefaultAsync(i => i.Path == path && i.NamespaceId == namespaceId);

        if (entity == null)
            throw new EntityNotFoundException($"NamespaceAdditionalTriggerPath with path {path} not found.");

        return entity;
    }
}
