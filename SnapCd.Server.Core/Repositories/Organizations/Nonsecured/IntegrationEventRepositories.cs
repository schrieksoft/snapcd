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
using SnapCd.Contracts.Dto.IntegrationEvents;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.IntegrationEvents;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

// ── Organization ──

public class OrganizationIntegrationEventRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<OrganizationIntegrationEventRepositorySettings> options)
{
    public OrganizationIntegrationEventRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new OrganizationIntegrationEventRepository(dbContext, principalProvider, bus, options);
    }
}

public class OrganizationIntegrationEventRepository : GenericOrganizationChildRepository<OrganizationIntegrationEvent, OrganizationIntegrationEventReadDto, OrganizationIntegrationEventCreatedEvent, OrganizationIntegrationEventUpdatedEvent, OrganizationIntegrationEventDeletedEvent, OrganizationIntegrationEventRepositorySettings>
{
    public OrganizationIntegrationEventRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<OrganizationIntegrationEventRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override OrganizationIntegrationEventReadDto MapToDto(OrganizationIntegrationEvent entity) => OrganizationIntegrationEventMapper.ToDto(entity);

    public async Task<List<OrganizationIntegrationEvent>> ListByIntegration(Guid integrationId, Guid organizationId)
    {
        return await DbContext.OrganizationIntegrationEvents
            .Where(e => e.OrganizationId == organizationId && e.IntegrationId == integrationId)
            .ToListAsync();
    }
}

// ── Stack ──

public class StackIntegrationEventRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<StackIntegrationEventRepositorySettings> options)
{
    public StackIntegrationEventRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new StackIntegrationEventRepository(dbContext, principalProvider, bus, options);
    }
}

public class StackIntegrationEventRepository : GenericStackChildRepository<StackIntegrationEvent, StackIntegrationEventReadDto, StackIntegrationEventCreatedEvent, StackIntegrationEventUpdatedEvent, StackIntegrationEventDeletedEvent, StackIntegrationEventRepositorySettings>
{
    public StackIntegrationEventRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<StackIntegrationEventRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override StackIntegrationEventReadDto MapToDto(StackIntegrationEvent entity) => StackIntegrationEventMapper.ToDto(entity);

    public async Task<List<StackIntegrationEvent>> ListByIntegration(Guid integrationId, Guid organizationId)
    {
        return await DbContext.StackIntegrationEvents
            .Where(e => e.OrganizationId == organizationId && e.IntegrationId == integrationId)
            .ToListAsync();
    }

    public async Task<List<StackIntegrationEvent>> ListByStack(Guid stackId, Guid organizationId)
    {
        return await DbContext.StackIntegrationEvents
            .Where(e => e.OrganizationId == organizationId && e.StackId == stackId)
            .ToListAsync();
    }
}

// ── Namespace ──

public class NamespaceIntegrationEventRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<NamespaceIntegrationEventRepositorySettings> options)
{
    public NamespaceIntegrationEventRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceIntegrationEventRepository(dbContext, principalProvider, bus, options);
    }
}

public class NamespaceIntegrationEventRepository : GenericNamespaceChildRepository<NamespaceIntegrationEvent, NamespaceIntegrationEventReadDto, NamespaceIntegrationEventCreatedEvent, NamespaceIntegrationEventUpdatedEvent, NamespaceIntegrationEventDeletedEvent, NamespaceIntegrationEventRepositorySettings>
{
    public NamespaceIntegrationEventRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<NamespaceIntegrationEventRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override NamespaceIntegrationEventReadDto MapToDto(NamespaceIntegrationEvent entity) => NamespaceIntegrationEventMapper.ToDto(entity);

    public async Task<List<NamespaceIntegrationEvent>> ListByIntegration(Guid integrationId, Guid organizationId)
    {
        return await DbContext.NamespaceIntegrationEvents
            .Where(e => e.OrganizationId == organizationId && e.IntegrationId == integrationId)
            .ToListAsync();
    }

    public async Task<List<NamespaceIntegrationEvent>> ListByNamespace(Guid namespaceId, Guid organizationId)
    {
        return await DbContext.NamespaceIntegrationEvents
            .Where(e => e.OrganizationId == organizationId && e.NamespaceId == namespaceId)
            .ToListAsync();
    }
}

// ── Module ──

public class ModuleIntegrationEventRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModuleIntegrationEventRepositorySettings> options)
{
    public ModuleIntegrationEventRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleIntegrationEventRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModuleIntegrationEventRepository : GenericModuleChildRepository<ModuleIntegrationEvent, ModuleIntegrationEventReadDto, ModuleIntegrationEventCreatedEvent, ModuleIntegrationEventUpdatedEvent, ModuleIntegrationEventDeletedEvent, ModuleIntegrationEventRepositorySettings>
{
    public ModuleIntegrationEventRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleIntegrationEventRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleIntegrationEventReadDto MapToDto(ModuleIntegrationEvent entity) => ModuleIntegrationEventMapper.ToDto(entity);

    public async Task<List<ModuleIntegrationEvent>> ListByIntegration(Guid integrationId, Guid organizationId)
    {
        return await DbContext.ModuleIntegrationEvents
            .Where(e => e.OrganizationId == organizationId && e.IntegrationId == integrationId)
            .ToListAsync();
    }

    public async Task<List<ModuleIntegrationEvent>> ListByModule(Guid moduleId, Guid organizationId)
    {
        return await DbContext.ModuleIntegrationEvents
            .Where(e => e.OrganizationId == organizationId && e.ModuleId == moduleId)
            .ToListAsync();
    }
}
