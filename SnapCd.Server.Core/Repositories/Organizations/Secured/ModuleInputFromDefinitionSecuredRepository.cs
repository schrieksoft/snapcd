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
using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ModuleInputFromDefinitionSecuredRepositoryFactory<TEntity>(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModuleInputFromDefinitionRepositorySettings> options)
    where TEntity : ModuleInput, IModuleInputFromDefinition
{
    public ModuleInputFromDefinitionSecuredRepository<TEntity> Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleInputFromDefinitionSecuredRepository<TEntity>(
            new ModuleInputFromDefinitionRepository<TEntity>(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ModuleInputFromDefinitionSecuredRepository<TEntity> : GenericModuleChildSecuredRepository<
    TEntity,
    ModuleInputFromDefinitionReadDto,
    ModuleInputFromDefinitionRepository<TEntity>,
    ModuleInputFromDefinitionCreatedEvent,
    ModuleInputFromDefinitionUpdatedEvent,
    ModuleInputFromDefinitionDeletedEvent,
    ModuleInputFromDefinitionRepositorySettings>
    where TEntity : ModuleInput, IModuleInputFromDefinition
{
    public ModuleInputFromDefinitionSecuredRepository(
        ModuleInputFromDefinitionRepository<TEntity> repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<TEntity> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await Repository.Get(moduleId, name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to {typeof(TEntity).Name} {entity.Id}");

        return entity;
    }
}