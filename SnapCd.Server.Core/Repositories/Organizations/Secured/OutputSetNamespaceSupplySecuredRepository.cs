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
using SnapCd.Contracts.Dto.OutputSetNamespaceSupplies;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.OutputSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Services.Interfaces.PrincipalProvider;
using SnapCd.Server.Core.Services.Nonsecured.PrincipalProvider;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class OutputSetNamespaceSupplySecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<OutputSetNamespaceSupplyRepositorySettings> options)
{
    public OutputSetNamespaceSupplySecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new OutputSetNamespaceSupplySecuredRepository(
            new OutputSetNamespaceSupplyRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class OutputSetNamespaceSupplySecuredRepository : GenericModuleChildSecuredRepository<
    OutputSetNamespaceSupply,
    OutputSetNamespaceSupplyReadDto, OutputSetNamespaceSupplyMetadata,
    OutputSetNamespaceSupplyRepository,
    OutputSetNamespaceSupplyCreatedEvent,
    OutputSetNamespaceSupplyUpdatedEvent,
    OutputSetNamespaceSupplyDeletedEvent,
    OutputSetNamespaceSupplyRepositorySettings>
{
    public OutputSetNamespaceSupplySecuredRepository(
        OutputSetNamespaceSupplyRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<List<OutputSetNamespaceSupply>> ListByModule(Guid moduleId, Guid organizationId)
    {
        return await Repository.ListByModule(moduleId, organizationId);
    }
}
