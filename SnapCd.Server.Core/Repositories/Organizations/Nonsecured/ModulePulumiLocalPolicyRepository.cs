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
using SnapCd.Contracts.Dto.ModulePulumiLocalPolicies;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using System.Linq.Expressions;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ModulePulumiLocalPolicyRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModulePulumiLocalPolicyRepositorySettings> options)
{
    public ModulePulumiLocalPolicyRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModulePulumiLocalPolicyRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModulePulumiLocalPolicyRepository : GenericModuleChildDefinitionRepository<ModulePulumiLocalPolicy, ModulePulumiLocalPolicyReadDto, ModulePulumiLocalPolicyMetadata, ModulePulumiLocalPolicyCreatedEvent, ModulePulumiLocalPolicyUpdatedEvent,
    ModulePulumiLocalPolicyDeletedEvent, ModulePulumiLocalPolicyRepositorySettings>
{

    protected override Expression<Func<ModulePulumiLocalPolicy, ModulePulumiLocalPolicyMetadata>> MetadataProjection =>
        e => new ModulePulumiLocalPolicyMetadata
        {
            Id = e.Id,
            OrganizationId = e.OrganizationId,
            Name = e.Name,
            ModuleId = e.ModuleId
        };
    public ModulePulumiLocalPolicyRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModulePulumiLocalPolicyRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModulePulumiLocalPolicyReadDto MapToDto(ModulePulumiLocalPolicy entity)
    {
        return ModulePulumiLocalPolicyMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ModulePulumiLocalPolicy entity)
    {
        var currentCount = await DbContext.ModulePulumiLocalPolicies
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ModulePulumiLocalPolicyQuota), currentCount);
    }

    public async Task<ModulePulumiLocalPolicy> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await DbContext.ModulePulumiLocalPolicies
            .SingleOrDefaultAsync(e => e.Name == name && e.ModuleId == moduleId && e.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException($"ModulePulumiLocalPolicy with name {name} not found for module {moduleId}.");

        return entity;
    }
}
