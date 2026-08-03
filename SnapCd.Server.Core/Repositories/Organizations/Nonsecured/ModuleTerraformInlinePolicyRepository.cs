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
using SnapCd.Contracts.Dto.ModuleTerraformInlinePolicies;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ModuleTerraformInlinePolicyRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModuleTerraformInlinePolicyRepositorySettings> options)
{
    public ModuleTerraformInlinePolicyRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleTerraformInlinePolicyRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModuleTerraformInlinePolicyRepository : GenericModuleChildDefinitionRepository<ModuleTerraformInlinePolicy, ModuleTerraformInlinePolicyReadDto, ModuleTerraformInlinePolicyCreatedEvent, ModuleTerraformInlinePolicyUpdatedEvent,
    ModuleTerraformInlinePolicyDeletedEvent, ModuleTerraformInlinePolicyRepositorySettings>
{
    public ModuleTerraformInlinePolicyRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleTerraformInlinePolicyRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleTerraformInlinePolicyReadDto MapToDto(ModuleTerraformInlinePolicy entity)
    {
        return ModuleTerraformInlinePolicyMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ModuleTerraformInlinePolicy entity)
    {
        var currentCount = await DbContext.ModuleTerraformInlinePolicies
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ModuleTerraformInlinePolicyQuota), currentCount);
    }

    public async Task<ModuleTerraformInlinePolicy> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await DbContext.ModuleTerraformInlinePolicies
            .SingleOrDefaultAsync(e => e.Name == name && e.ModuleId == moduleId && e.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException($"ModuleTerraformInlinePolicy with name {name} not found for module {moduleId}.");

        return entity;
    }
}
