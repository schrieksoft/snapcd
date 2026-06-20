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
using SnapCd.Contracts.Dto.Integrations;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.IntegrationSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class IntegrationModuleSupplyRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationSupplyRepositorySettings> options)
{
    public IntegrationModuleSupplyRepository Create(IPrincipalProvider? principalProvider = null)
        => new(dbFactory.CreateDbContext(), principalProvider ?? new HttpContextPrincipalProvider(new HttpContextAccessor()), bus, options);
}

public class IntegrationModuleSupplyRepository(
    SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<IntegrationSupplyRepositorySettings> options)
    : GenericOrganizationChildRepository<IntegrationModuleSupply, IntegrationSupplyDto,
        IntegrationSupplyCreatedEvent, IntegrationSupplyUpdatedEvent, IntegrationSupplyDeletedEvent,
        IntegrationSupplyRepositorySettings>(dbContext, principalProvider, bus, options)
{
    protected override IntegrationSupplyDto MapToDto(IntegrationModuleSupply entity)
        => IntegrationSupplyMapper.ToDto(entity);
}

public class IntegrationNamespaceSupplyRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationSupplyRepositorySettings> options)
{
    public IntegrationNamespaceSupplyRepository Create(IPrincipalProvider? principalProvider = null)
        => new(dbFactory.CreateDbContext(), principalProvider ?? new HttpContextPrincipalProvider(new HttpContextAccessor()), bus, options);
}

public class IntegrationNamespaceSupplyRepository(
    SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<IntegrationSupplyRepositorySettings> options)
    : GenericOrganizationChildRepository<IntegrationNamespaceSupply, IntegrationSupplyDto,
        IntegrationSupplyCreatedEvent, IntegrationSupplyUpdatedEvent, IntegrationSupplyDeletedEvent,
        IntegrationSupplyRepositorySettings>(dbContext, principalProvider, bus, options)
{
    protected override IntegrationSupplyDto MapToDto(IntegrationNamespaceSupply entity)
        => IntegrationSupplyMapper.ToDto(entity);
}

public class IntegrationStackSupplyRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationSupplyRepositorySettings> options)
{
    public IntegrationStackSupplyRepository Create(IPrincipalProvider? principalProvider = null)
        => new(dbFactory.CreateDbContext(), principalProvider ?? new HttpContextPrincipalProvider(new HttpContextAccessor()), bus, options);
}

public class IntegrationStackSupplyRepository(
    SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<IntegrationSupplyRepositorySettings> options)
    : GenericOrganizationChildRepository<IntegrationStackSupply, IntegrationSupplyDto,
        IntegrationSupplyCreatedEvent, IntegrationSupplyUpdatedEvent, IntegrationSupplyDeletedEvent,
        IntegrationSupplyRepositorySettings>(dbContext, principalProvider, bus, options)
{
    protected override IntegrationSupplyDto MapToDto(IntegrationStackSupply entity)
        => IntegrationSupplyMapper.ToDto(entity);
}
