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
using SnapCd.Contracts.Dto.IntegrationModuleSupplies;
using SnapCd.Contracts.Dto.IntegrationNamespaceSupplies;
using SnapCd.Contracts.Dto.IntegrationStackSupplies;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.IntegrationSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class IntegrationModuleSupplyRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationModuleSupplyRepositorySettings> options)
{
    public IntegrationModuleSupplyRepository Create(IPrincipalProvider? principalProvider = null)
        => new(dbFactory.CreateDbContext(), principalProvider ?? new HttpContextPrincipalProvider(new HttpContextAccessor()), bus, options);
}

public class IntegrationModuleSupplyRepository(
    SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<IntegrationModuleSupplyRepositorySettings> options)
    : GenericIntegrationChildRepository<IntegrationModuleSupply, IntegrationModuleSupplyReadDto,
        IntegrationModuleSupplyCreatedEvent, IntegrationModuleSupplyUpdatedEvent, IntegrationModuleSupplyDeletedEvent,
        IntegrationModuleSupplyRepositorySettings>(dbContext, principalProvider, bus, options)
{
    protected override IntegrationModuleSupplyReadDto MapToDto(IntegrationModuleSupply entity)
        => IntegrationModuleSupplyMapper.ToDto(entity);
}

public class IntegrationNamespaceSupplyRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationNamespaceSupplyRepositorySettings> options)
{
    public IntegrationNamespaceSupplyRepository Create(IPrincipalProvider? principalProvider = null)
        => new(dbFactory.CreateDbContext(), principalProvider ?? new HttpContextPrincipalProvider(new HttpContextAccessor()), bus, options);
}

public class IntegrationNamespaceSupplyRepository(
    SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<IntegrationNamespaceSupplyRepositorySettings> options)
    : GenericIntegrationChildRepository<IntegrationNamespaceSupply, IntegrationNamespaceSupplyReadDto,
        IntegrationNamespaceSupplyCreatedEvent, IntegrationNamespaceSupplyUpdatedEvent, IntegrationNamespaceSupplyDeletedEvent,
        IntegrationNamespaceSupplyRepositorySettings>(dbContext, principalProvider, bus, options)
{
    protected override IntegrationNamespaceSupplyReadDto MapToDto(IntegrationNamespaceSupply entity)
        => IntegrationNamespaceSupplyMapper.ToDto(entity);
}

public class IntegrationStackSupplyRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationStackSupplyRepositorySettings> options)
{
    public IntegrationStackSupplyRepository Create(IPrincipalProvider? principalProvider = null)
        => new(dbFactory.CreateDbContext(), principalProvider ?? new HttpContextPrincipalProvider(new HttpContextAccessor()), bus, options);
}

public class IntegrationStackSupplyRepository(
    SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<IntegrationStackSupplyRepositorySettings> options)
    : GenericIntegrationChildRepository<IntegrationStackSupply, IntegrationStackSupplyReadDto,
        IntegrationStackSupplyCreatedEvent, IntegrationStackSupplyUpdatedEvent, IntegrationStackSupplyDeletedEvent,
        IntegrationStackSupplyRepositorySettings>(dbContext, principalProvider, bus, options)
{
    protected override IntegrationStackSupplyReadDto MapToDto(IntegrationStackSupply entity)
        => IntegrationStackSupplyMapper.ToDto(entity);
}
