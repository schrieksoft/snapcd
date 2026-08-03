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
using SnapCd.Contracts.Dto.ModuleTerraformLocalPolicies;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ModuleTerraformLocalPolicyRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModuleTerraformLocalPolicyRepositorySettings> options)
{
    public ModuleTerraformLocalPolicyRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleTerraformLocalPolicyRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModuleTerraformLocalPolicyRepository : GenericModuleChildDefinitionRepository<ModuleTerraformLocalPolicy, ModuleTerraformLocalPolicyReadDto, ModuleTerraformLocalPolicyCreatedEvent, ModuleTerraformLocalPolicyUpdatedEvent,
    ModuleTerraformLocalPolicyDeletedEvent, ModuleTerraformLocalPolicyRepositorySettings>
{
    public ModuleTerraformLocalPolicyRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleTerraformLocalPolicyRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleTerraformLocalPolicyReadDto MapToDto(ModuleTerraformLocalPolicy entity)
    {
        return ModuleTerraformLocalPolicyMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ModuleTerraformLocalPolicy entity)
    {
        var currentCount = await DbContext.ModuleTerraformLocalPolicies
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ModuleTerraformLocalPolicyQuota), currentCount);
    }

    public async Task<ModuleTerraformLocalPolicy> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await DbContext.ModuleTerraformLocalPolicies
            .SingleOrDefaultAsync(e => e.Name == name && e.ModuleId == moduleId && e.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException($"ModuleTerraformLocalPolicy with name {name} not found for module {moduleId}.");

        return entity;
    }
}
