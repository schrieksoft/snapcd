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
using SnapCd.Contracts.Dto.Missions;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

/// <summary>
/// Creates a <see cref="ModuleJobMissionRunRepository"/>. The <c>Create</c> overload takes an optional
/// <see cref="IPrincipalProvider"/> so callers outside an HTTP request — notably the SignalR
/// <c>AgentHub</c> and the Layer-1 dispatcher — can supply a <see cref="ClaimsPrincipalProvider"/>
/// (built from the connection's <c>Context.User</c>) so audit fields attribute to the connecting agent.
/// </summary>
public class ModuleJobMissionRunRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModuleJobMissionRunRepositorySettings> options)
{
    public ModuleJobMissionRunRepository Create(IPrincipalProvider? principalProvider = null, bool suppressEvents = false)
    {
        principalProvider ??= new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleJobMissionRunRepository(dbContext, principalProvider, bus, options, suppressEvents);
    }
}

public class ModuleJobMissionRunRepository : GenericOrganizationChildRepository<
    ModuleJobMissionRun, ModuleJobMissionRunReadDto,
    ModuleJobMissionRunCreatedEvent, ModuleJobMissionRunUpdatedEvent, ModuleJobMissionRunDeletedEvent,
    ModuleJobMissionRunRepositorySettings>
{
    public ModuleJobMissionRunRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleJobMissionRunRepositorySettings> options,
        bool suppressEvents = false)
        : base(dbContext, principalProvider, bus, options, suppressEvents: suppressEvents)
    {
    }

    protected override ModuleJobMissionRunReadDto MapToDto(ModuleJobMissionRun entity)
        => ModuleJobMissionRunMapper.ToDto(entity);
}
