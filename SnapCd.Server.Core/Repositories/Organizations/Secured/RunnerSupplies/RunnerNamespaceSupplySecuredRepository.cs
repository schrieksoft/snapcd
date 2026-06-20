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
using SnapCd.Contracts.Dto.RunnerNamespaceSupplies;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerSupplies;

public class RunnerNamespaceSupplySecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<RunnerNamespaceSupplyRepositorySettings> options)
{
    public RunnerNamespaceSupplySecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new RunnerNamespaceSupplySecuredRepository(
            new RunnerNamespaceSupplyRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class RunnerNamespaceSupplySecuredRepository : GenericRunnerChildSecuredRepository<
    RunnerNamespaceSupply,
    RunnerNamespaceSupplyReadDto,
    RunnerNamespaceSupplyRepository,
    RunnerNamespaceSupplyCreatedEvent,
    RunnerNamespaceSupplyUpdatedEvent,
    RunnerNamespaceSupplyDeletedEvent,
    RunnerNamespaceSupplyRepositorySettings>
{
    public RunnerNamespaceSupplySecuredRepository(
        RunnerNamespaceSupplyRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<List<RunnerNamespaceSupply>> ListByRunner(Guid runnerId, Guid organizationId)
    {
        return await Repository.ListByRunner(runnerId, organizationId);
    }

    public async Task<List<RunnerNamespaceSupply>> ListByNamespace(Guid namespaceId, Guid organizationId)
    {
        return await Repository.ListByNamespace(namespaceId, organizationId);
    }
}