// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Linq.Expressions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.SignalOutputSets;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Signals;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.Interfaces.PrincipalProvider;
using SnapCd.Server.Core.Services.Nonsecured.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Signals;

public class SignalOutputSetRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<SignalOutputSetRepositorySettings> options)
{
    public SignalOutputSetRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new SignalOutputSetRepository(dbContext, principalProvider, bus, options);
    }
}

/// <summary>
/// A set of probe signals recorded for one Module. Deleting one raises a DeletedEvent carrying the
/// Module, which is what lets the sensitive signals' values be removed from the vault: they outlive
/// their rows otherwise, since a delete is a SQL cascade and makes no vault call.
/// </summary>
public class SignalOutputSetRepository : GenericModuleChildRepository<
    SignalOutputSet,
    SignalOutputSetReadDto,
    SignalOutputSetMetadata,
    SignalOutputSetCreatedEvent,
    SignalOutputSetUpdatedEvent,
    SignalOutputSetDeletedEvent,
    SignalOutputSetRepositorySettings>
{
    protected override Expression<Func<SignalOutputSet, SignalOutputSetMetadata>> MetadataProjection =>
        e => new SignalOutputSetMetadata
        {
            Id = e.Id,
            OrganizationId = e.OrganizationId,
            ModuleId = e.ModuleId
        };

    public SignalOutputSetRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<SignalOutputSetRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override SignalOutputSetReadDto MapToDto(SignalOutputSet entity)
    {
        return SignalOutputSetMapper.ToDto(entity);
    }
}
