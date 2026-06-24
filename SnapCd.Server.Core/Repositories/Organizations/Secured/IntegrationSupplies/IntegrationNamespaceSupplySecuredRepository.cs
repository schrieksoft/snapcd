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
using SnapCd.Contracts.Dto.Integrations;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.IntegrationSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.IntegrationSupplies;

public class IntegrationNamespaceSupplySecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<IntegrationSupplyRepositorySettings> options)
{
    public IntegrationNamespaceSupplySecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new IntegrationNamespaceSupplySecuredRepository(
            new IntegrationNamespaceSupplyRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class IntegrationNamespaceSupplySecuredRepository : GenericIntegrationChildSecuredRepository<
    IntegrationNamespaceSupply,
    IntegrationSupplyDto,
    IntegrationNamespaceSupplyRepository,
    IntegrationSupplyCreatedEvent,
    IntegrationSupplyUpdatedEvent,
    IntegrationSupplyDeletedEvent,
    IntegrationSupplyRepositorySettings>
{
    public IntegrationNamespaceSupplySecuredRepository(
        IntegrationNamespaceSupplyRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<List<IntegrationNamespaceSupply>> ListByIntegration(Guid integrationId, Guid organizationId)
    {
        return await ReadQuery(organizationId)
            .Where(a => a.IntegrationId == integrationId)
            .ToListAsync();
    }

    public async Task<List<IntegrationNamespaceSupply>> ListByNamespace(Guid namespaceId, Guid organizationId)
    {
        return await ReadQuery(organizationId)
            .Where(a => a.NamespaceId == namespaceId)
            .ToListAsync();
    }
}
