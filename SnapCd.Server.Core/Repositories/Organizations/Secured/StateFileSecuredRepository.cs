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
using SnapCd.Contracts.Dto.StateFiles;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class StateFileSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<StateFileRepositorySettings> options)
{
    public StateFileSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new StateFileSecuredRepository(
            new StateFileRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class StateFileSecuredRepository : GenericStateStoreChildSecuredRepository<
    StateFile,
    StateFileReadDto,
    StateFileRepository,
    StateFileCreatedEvent,
    StateFileUpdatedEvent,
    StateFileDeletedEvent,
    StateFileRepositorySettings>
{
    public StateFileSecuredRepository(
        StateFileRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<StateFile> GetByName(string name, Guid stateStoreId, Guid organizationId)
    {
        var entity = await Repository.GetByName(name, stateStoreId);

        if (!CanRead(entity.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(StateFile)} with name {name} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return entity;
    }

    public async Task<StateFile?> FindByName(string name, Guid stateStoreId)
    {
        return await Repository.FindByName(name, stateStoreId);
    }
}
