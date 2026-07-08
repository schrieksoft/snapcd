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
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.StateStores;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class StateStoreRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<StateStoreRepositorySettings> options)
{
    public StateStoreRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new StateStoreRepository(dbContext, principalProvider, bus, options);
    }
}

public class StateStoreRepository : GenericOrganizationChildRepository<StateStore, StateStoreReadDto, StateStoreCreatedEvent, StateStoreUpdatedEvent, StateStoreDeletedEvent, StateStoreRepositorySettings>
{
    public StateStoreRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<StateStoreRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override async Task SetServicePrincipalOwner(Guid id, Guid organizationId, Guid servicePrincipalId)
    {
        DbContext.Set<ServicePrincipalStateStoreRoleAssignment>().Add(new ServicePrincipalStateStoreRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StateStoreId = id,
            ServicePrincipalId = servicePrincipalId,
            RoleName = StateStoreRole.Owner
        });
    }

    protected override async Task SetUserOwner(Guid id, Guid organizationId, Guid userId)
    {
        DbContext.Set<UserStateStoreRoleAssignment>().Add(new UserStateStoreRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            StateStoreId = id,
            UserId = userId,
            RoleName = StateStoreRole.Owner
        });
    }

    protected override StateStoreReadDto MapToDto(StateStore entity)
    {
        return StateStoreMapper.ToDto(entity);
    }

    public async Task<StateStore> GetByName(string name, Guid organizationId)
    {
        var entity = await DbContext.Set<StateStore>()
            .Where(s => s.OrganizationId == organizationId)
            .SingleOrDefaultAsync(s => s.Name == name);

        if (entity == null) throw new EntityNotFoundException($"{nameof(StateStore)} with Name {name} not found.");

        return entity;
    }
}
