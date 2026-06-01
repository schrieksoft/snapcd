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
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos.PreviewFeatureAcceptances;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class PreviewFeatureAcceptanceSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<PreviewFeatureAcceptanceRepositorySettings> options)
{
    public PreviewFeatureAcceptanceSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new PreviewFeatureAcceptanceSecuredRepository(
            new PreviewFeatureAcceptanceRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class PreviewFeatureAcceptanceSecuredRepository : GenericOrganizationChildSecuredRepository<
    PreviewFeatureAcceptance,
    PreviewFeatureAcceptanceReadDto,
    PreviewFeatureAcceptanceRepository,
    PreviewFeatureAcceptanceCreatedEvent,
    PreviewFeatureAcceptanceUpdatedEvent,
    PreviewFeatureAcceptanceDeletedEvent,
    PreviewFeatureAcceptanceRepositorySettings>
{
    public PreviewFeatureAcceptanceSecuredRepository(
        PreviewFeatureAcceptanceRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner]
    };

    public async Task<PreviewFeatureAcceptance?> GetByFeature(PreviewFeature feature, Guid organizationId)
    {
        return await Repository.GetByFeature(feature, organizationId);
    }

    public async Task<List<PreviewFeatureAcceptance>> ListByOrganization(Guid organizationId)
    {
        return await Repository.ListByOrganization(organizationId);
    }
}
