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
using SnapCd.Contracts.Dto.ModuleTerraformFlags;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using System.Linq.Expressions;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ModuleTerraformFlagRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModuleTerraformFlagRepositorySettings> options)
{
    public ModuleTerraformFlagRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleTerraformFlagRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModuleTerraformFlagRepository : GenericModuleChildDefinitionRepository<ModuleTerraformFlag, ModuleTerraformFlagReadDto, ModuleTerraformFlagMetadata, ModuleTerraformFlagCreatedEvent, ModuleTerraformFlagUpdatedEvent,
    ModuleTerraformFlagDeletedEvent, ModuleTerraformFlagRepositorySettings>
{

    protected override Expression<Func<ModuleTerraformFlag, ModuleTerraformFlagMetadata>> MetadataProjection =>
        e => new ModuleTerraformFlagMetadata
        {
            Id = e.Id,
            OrganizationId = e.OrganizationId,
            ModuleId = e.ModuleId
        };
    public ModuleTerraformFlagRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleTerraformFlagRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleTerraformFlagReadDto MapToDto(ModuleTerraformFlag entity)
    {
        return ModuleTerraformFlagMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ModuleTerraformFlag entity)
    {
        var currentCount = await DbContext.ModuleTerraformFlags
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ModuleTerraformFlagQuota), currentCount);
    }
}
