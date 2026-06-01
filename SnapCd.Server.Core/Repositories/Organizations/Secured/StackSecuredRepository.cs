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
using SnapCd.Contracts.Dto.Stacks;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class StackSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<StackRepositorySettings> options)
{
    public StackSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new StackSecuredRepository(
            new StackRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class StackSecuredRepository : GenericOrganizationChildSecuredRepository<
    Stack,
    StackReadDto,
    StackRepository,
    StackCreatedEvent,
    StackUpdatedEvent,
    StackDeletedEvent,
    StackRepositorySettings>
{
    public StackSecuredRepository(
        StackRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<Stack> GetByName(string name, Guid organizationId)
    {
        var entity = await Repository.GetByName(name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(Stack)} with organization ID {organizationId} and name {name} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return entity;
    }
}