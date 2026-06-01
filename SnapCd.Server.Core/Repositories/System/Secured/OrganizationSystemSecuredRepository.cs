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
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos.Organizations;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.System;
using SnapCd.Server.Core.Repositories.System.Nonsecured;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Edition;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.System.Secured;

public class OrganizationSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<OrganizationRepositorySettings> options,
    IUserQuotaProvider userQuotaProvider,
    IOrganizationLimitPolicy organizationLimitPolicy)
{
    public OrganizationSystemSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        var repository = new OrganizationSystemRepository(dbContext, principalProvider, bus, options, userQuotaProvider, organizationLimitPolicy);
        return new OrganizationSystemSecuredRepository(repository, principalProvider);
    }
}

public class OrganizationSystemSecuredRepository : GenericSystemSecuredRepository<Entities.Definition.Organization, OrganizationReadDto, OrganizationSystemRepository, OrganizationCreatedEvent,
    OrganizationUpdatedEvent, OrganizationDeletedEvent, OrganizationRepositorySettings>
{
    public OrganizationSystemSecuredRepository(
        OrganizationSystemRepository systemRepository,
        IPrincipalProvider principalProvider)
        : base(systemRepository, principalProvider)
    {
    }

    protected override IQueryable<Entities.Definition.Organization> RoleQuery<TSystemRoleAssignment>(
        Guid principalId,
        List<SystemRole> systemRoles)
    {
        return SystemRepository.DbContext.Set<Entities.Definition.Organization>()
            .Where(org => SystemRepository.DbContext.Set<TSystemRoleAssignment>()
                .Any(ra => ra.PrincipalId == principalId && systemRoles.Contains(ra.RoleName)));
    }

    public async Task<List<Entities.Definition.Organization>> ListWithFilter(bool includeDeleted = false,
        Func<IQueryable<Entities.Definition.Organization>, IQueryable<Entities.Definition.Organization>>? queryFilter = null)
    {
        return await SystemRepository.ListWithFilter(includeDeleted, queryFilter);
    }

    public async Task<bool> SoftDelete(Guid id, Guid deletedByUserId)
    {
        return await SystemRepository.SoftDelete(id, deletedByUserId);
    }
}