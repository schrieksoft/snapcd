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
using SnapCd.Contracts.Dto.Integrations;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using System.Linq.Expressions;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

/// <summary>
/// Creates an <see cref="IntegrationRepository"/>. Non-secured so audit fields are stamped and CRUD events
/// emitted automatically; the CRUD events drive the fanout cache-invalidation consumer.
/// </summary>
public class IntegrationRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<IntegrationRepositorySettings> options)
{
    public IntegrationRepository Create(IPrincipalProvider? principalProvider = null)
    {
        principalProvider ??= new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new IntegrationRepository(dbContext, principalProvider, bus, options);
    }
}

public class IntegrationRepository : GenericOrganizationChildRepository<Integration, IntegrationReadDto, IntegrationMetadata,
    IntegrationCreatedEvent, IntegrationUpdatedEvent, IntegrationDeletedEvent,
    IntegrationRepositorySettings>
{

    protected override Expression<Func<Integration, IntegrationMetadata>> MetadataProjection =>
        e => new IntegrationMetadata
        {
            Id = e.Id,
            OrganizationId = e.OrganizationId,
            Name = e.Name,
            IntegrationType = e.IntegrationType
        };
    public IntegrationRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<IntegrationRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override IntegrationReadDto MapToDto(Integration entity)
        => IntegrationMapper.ToDto(entity);
}
