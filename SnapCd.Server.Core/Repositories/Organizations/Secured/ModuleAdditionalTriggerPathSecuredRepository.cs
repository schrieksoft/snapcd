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
using SnapCd.Contracts.Dto.ModuleAdditionalTriggerPaths;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ModuleAdditionalTriggerPathSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModuleAdditionalTriggerPathRepositorySettings> options)
{
    public ModuleAdditionalTriggerPathSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleAdditionalTriggerPathSecuredRepository(
            new ModuleAdditionalTriggerPathRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ModuleAdditionalTriggerPathSecuredRepository : GenericModuleChildSecuredRepository<
    ModuleAdditionalTriggerPath,
    ModuleAdditionalTriggerPathReadDto,
    ModuleAdditionalTriggerPathRepository,
    ModuleAdditionalTriggerPathCreatedEvent,
    ModuleAdditionalTriggerPathUpdatedEvent,
    ModuleAdditionalTriggerPathDeletedEvent,
    ModuleAdditionalTriggerPathRepositorySettings>
{
    public ModuleAdditionalTriggerPathSecuredRepository(
        ModuleAdditionalTriggerPathRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<ModuleAdditionalTriggerPath> Get(Guid moduleId, string path, Guid organizationId)
    {
        var entity = await Repository.Get(moduleId, path, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to ModuleAdditionalTriggerPath {entity.Id}");

        return entity;
    }
}
