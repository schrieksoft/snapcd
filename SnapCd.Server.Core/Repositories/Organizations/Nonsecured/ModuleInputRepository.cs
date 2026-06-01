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
using SnapCd.Contracts.Dto.ModuleInputs.Base;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ModuleInputRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModuleInputRepositorySettings> options)
{
    public ModuleInputRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleInputRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModuleInputRepository : GenericModuleChildDefinitionRepository<
    ModuleInput,
    ModuleInputReadDto,
    ModuleInputCreatedEvent,
    ModuleInputUpdatedEvent,
    ModuleInputDeletedEvent,
    ModuleInputRepositorySettings>
{
    public ModuleInputRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleInputRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleInputReadDto MapToDto(ModuleInput entity)
    {
        return ModuleInputMapper.ToDto(entity);
    }

    public async Task<ModuleInput> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await DbContext.ModuleInputs
            .SingleOrDefaultAsync(i => i.Name == name && i.ModuleId == moduleId && i.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException($"{nameof(ModuleInput)} with name {name} not found.");

        return entity;
    }
}