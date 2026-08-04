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
using SnapCd.Contracts.Dto.ModulePulumiRemotePolicies;
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

public class ModulePulumiRemotePolicyRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModulePulumiRemotePolicyRepositorySettings> options)
{
    public ModulePulumiRemotePolicyRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModulePulumiRemotePolicyRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModulePulumiRemotePolicyRepository : GenericModuleChildDefinitionRepository<ModulePulumiRemotePolicy, ModulePulumiRemotePolicyReadDto, ModulePulumiRemotePolicyCreatedEvent, ModulePulumiRemotePolicyUpdatedEvent,
    ModulePulumiRemotePolicyDeletedEvent, ModulePulumiRemotePolicyRepositorySettings>
{
    public ModulePulumiRemotePolicyRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModulePulumiRemotePolicyRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    public override async Task<ModulePulumiRemotePolicy> ExecuteCreate(ModulePulumiRemotePolicy entity)
    {
        if (!string.IsNullOrEmpty(entity.Path))
            TriggerPathValidator.EnsureValid(entity.Path);
        return await base.ExecuteCreate(entity);
    }

    public override async Task<ModulePulumiRemotePolicy> ExecuteUpdate(ModulePulumiRemotePolicy entity)
    {
        if (!string.IsNullOrEmpty(entity.Path))
            TriggerPathValidator.EnsureValid(entity.Path);
        return await base.ExecuteUpdate(entity);
    }

    protected override ModulePulumiRemotePolicyReadDto MapToDto(ModulePulumiRemotePolicy entity)
    {
        return ModulePulumiRemotePolicyMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ModulePulumiRemotePolicy entity)
    {
        var currentCount = await DbContext.ModulePulumiRemotePolicies
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ModulePulumiRemotePolicyQuota), currentCount);
    }

    public async Task<ModulePulumiRemotePolicy> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await DbContext.ModulePulumiRemotePolicies
            .SingleOrDefaultAsync(e => e.Name == name && e.ModuleId == moduleId && e.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException($"ModulePulumiRemotePolicy with name {name} not found for module {moduleId}.");

        return entity;
    }
}
