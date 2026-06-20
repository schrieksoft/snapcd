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
using SnapCd.Server.Core.Entities.Definition.IntegrationEvents;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class OrganizationIntegrationEventRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationEventRepositorySettings> options)
{
    public OrganizationIntegrationEventRepository Create(IPrincipalProvider? p = null)
        => new(dbFactory.CreateDbContext(), p ?? new HttpContextPrincipalProvider(new HttpContextAccessor()), bus, options);
}

public class OrganizationIntegrationEventRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<IntegrationEventRepositorySettings> options)
    : GenericOrganizationChildRepository<OrganizationIntegrationEvent, IntegrationEventDto,
        IntegrationEventCreatedEvent, IntegrationEventUpdatedEvent, IntegrationEventDeletedEvent, IntegrationEventRepositorySettings>(dbContext, principalProvider, bus, options)
{
    protected override IntegrationEventDto MapToDto(OrganizationIntegrationEvent entity) => IntegrationEventMapper.ToDto(entity);
}

public class StackIntegrationEventRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationEventRepositorySettings> options)
{
    public StackIntegrationEventRepository Create(IPrincipalProvider? p = null)
        => new(dbFactory.CreateDbContext(), p ?? new HttpContextPrincipalProvider(new HttpContextAccessor()), bus, options);
}

public class StackIntegrationEventRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<IntegrationEventRepositorySettings> options)
    : GenericOrganizationChildRepository<StackIntegrationEvent, IntegrationEventDto,
        IntegrationEventCreatedEvent, IntegrationEventUpdatedEvent, IntegrationEventDeletedEvent, IntegrationEventRepositorySettings>(dbContext, principalProvider, bus, options)
{
    protected override IntegrationEventDto MapToDto(StackIntegrationEvent entity) => IntegrationEventMapper.ToDto(entity);
}

public class NamespaceIntegrationEventRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationEventRepositorySettings> options)
{
    public NamespaceIntegrationEventRepository Create(IPrincipalProvider? p = null)
        => new(dbFactory.CreateDbContext(), p ?? new HttpContextPrincipalProvider(new HttpContextAccessor()), bus, options);
}

public class NamespaceIntegrationEventRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<IntegrationEventRepositorySettings> options)
    : GenericOrganizationChildRepository<NamespaceIntegrationEvent, IntegrationEventDto,
        IntegrationEventCreatedEvent, IntegrationEventUpdatedEvent, IntegrationEventDeletedEvent, IntegrationEventRepositorySettings>(dbContext, principalProvider, bus, options)
{
    protected override IntegrationEventDto MapToDto(NamespaceIntegrationEvent entity) => IntegrationEventMapper.ToDto(entity);
}

public class ModuleIntegrationEventRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationEventRepositorySettings> options)
{
    public ModuleIntegrationEventRepository Create(IPrincipalProvider? p = null)
        => new(dbFactory.CreateDbContext(), p ?? new HttpContextPrincipalProvider(new HttpContextAccessor()), bus, options);
}

public class ModuleIntegrationEventRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<IntegrationEventRepositorySettings> options)
    : GenericOrganizationChildRepository<ModuleIntegrationEvent, IntegrationEventDto,
        IntegrationEventCreatedEvent, IntegrationEventUpdatedEvent, IntegrationEventDeletedEvent, IntegrationEventRepositorySettings>(dbContext, principalProvider, bus, options)
{
    protected override IntegrationEventDto MapToDto(ModuleIntegrationEvent entity) => IntegrationEventMapper.ToDto(entity);
}
