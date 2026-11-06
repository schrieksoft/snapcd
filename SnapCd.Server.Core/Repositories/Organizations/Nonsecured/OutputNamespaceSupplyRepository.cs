// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.OutputNamespaceSupplies;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.OutputSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Settings.Repositories;
using System.Linq.Expressions;
using SnapCd.Server.Core.Services.Interfaces.PrincipalProvider;
using SnapCd.Server.Core.Services.Nonsecured.PrincipalProvider;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class OutputNamespaceSupplyRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<OutputNamespaceSupplyRepositorySettings> options)
{
    public OutputNamespaceSupplyRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new OutputNamespaceSupplyRepository(dbContext, principalProvider, bus, options);
    }
}

public class OutputNamespaceSupplyRepository : GenericModuleChildRepository<OutputNamespaceSupply, OutputNamespaceSupplyReadDto, OutputNamespaceSupplyMetadata,
    OutputNamespaceSupplyCreatedEvent, OutputNamespaceSupplyUpdatedEvent, OutputNamespaceSupplyDeletedEvent, OutputNamespaceSupplyRepositorySettings>
{
    protected override Expression<Func<OutputNamespaceSupply, OutputNamespaceSupplyMetadata>> MetadataProjection =>
        e => new OutputNamespaceSupplyMetadata
        {
            Id = e.Id,
            OrganizationId = e.OrganizationId,
            ModuleId = e.ModuleId
        };

    public OutputNamespaceSupplyRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<OutputNamespaceSupplyRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override OutputNamespaceSupplyReadDto MapToDto(OutputNamespaceSupply entity)
    {
        return OutputNamespaceSupplyMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(OutputNamespaceSupply entity)
    {
        var currentCount = await DbContext.OutputNamespaceSupplies
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.OutputNamespaceSupplyQuota), currentCount);
    }

    public async Task<List<OutputNamespaceSupply>> ListByModule(Guid moduleId, Guid organizationId)
    {
        return await DbContext.OutputNamespaceSupplies
            .Where(e => e.OrganizationId == organizationId)
            .Where(e => e.ModuleId == moduleId)
            .ToListAsync();
    }
}
