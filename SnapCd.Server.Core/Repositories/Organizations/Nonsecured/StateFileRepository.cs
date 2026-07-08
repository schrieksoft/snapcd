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
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class StateFileRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<StateFileRepositorySettings> options)
{
    public StateFileRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new StateFileRepository(dbContext, principalProvider, bus, options);
    }
}

public class StateFileRepository : GenericStateStoreChildRepository<StateFile, StateFileReadDto, StateFileCreatedEvent, StateFileUpdatedEvent, StateFileDeletedEvent, StateFileRepositorySettings>
{
    public StateFileRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<StateFileRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override StateFileReadDto MapToDto(StateFile entity)
    {
        return StateFileMapper.ToDto(entity);
    }

    public async Task<StateFile> GetByName(string name, Guid stateStoreId)
    {
        var entity = await DbContext.Set<StateFile>()
            .Where(s => s.StateStoreId == stateStoreId)
            .SingleOrDefaultAsync(s => s.Name == name);

        if (entity == null) throw new EntityNotFoundException($"{nameof(StateFile)} with Name {name} not found.");

        return entity;
    }

    public async Task<StateFile?> FindByName(string name, Guid stateStoreId)
    {
        return await DbContext.Set<StateFile>()
            .Where(s => s.StateStoreId == stateStoreId)
            .SingleOrDefaultAsync(s => s.Name == name);
    }
}
