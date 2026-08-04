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
using SnapCd.Contracts.Dto.ModuleTerraformRemotePolicies;
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

public class ModuleTerraformRemotePolicyRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModuleTerraformRemotePolicyRepositorySettings> options)
{
    public ModuleTerraformRemotePolicyRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleTerraformRemotePolicyRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModuleTerraformRemotePolicyRepository : GenericModuleChildDefinitionRepository<ModuleTerraformRemotePolicy, ModuleTerraformRemotePolicyReadDto, ModuleTerraformRemotePolicyCreatedEvent, ModuleTerraformRemotePolicyUpdatedEvent,
    ModuleTerraformRemotePolicyDeletedEvent, ModuleTerraformRemotePolicyRepositorySettings>
{
    public ModuleTerraformRemotePolicyRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleTerraformRemotePolicyRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    public override async Task<ModuleTerraformRemotePolicy> ExecuteCreate(ModuleTerraformRemotePolicy entity)
    {
        if (!string.IsNullOrEmpty(entity.Path))
            TriggerPathValidator.EnsureValid(entity.Path);
        return await base.ExecuteCreate(entity);
    }

    public override async Task<ModuleTerraformRemotePolicy> ExecuteUpdate(ModuleTerraformRemotePolicy entity)
    {
        if (!string.IsNullOrEmpty(entity.Path))
            TriggerPathValidator.EnsureValid(entity.Path);
        return await base.ExecuteUpdate(entity);
    }

    protected override ModuleTerraformRemotePolicyReadDto MapToDto(ModuleTerraformRemotePolicy entity)
    {
        return ModuleTerraformRemotePolicyMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ModuleTerraformRemotePolicy entity)
    {
        var currentCount = await DbContext.ModuleTerraformRemotePolicies
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ModuleTerraformRemotePolicyQuota), currentCount);
    }

    public async Task<ModuleTerraformRemotePolicy> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await DbContext.ModuleTerraformRemotePolicies
            .SingleOrDefaultAsync(e => e.Name == name && e.ModuleId == moduleId && e.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException($"ModuleTerraformRemotePolicy with name {name} not found for module {moduleId}.");

        return entity;
    }
}
