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
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.IntegrationEvents;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.IntegrationEvents;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class StackIntegrationEventSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<StackIntegrationEventRepositorySettings> options)
{
    public StackIntegrationEventSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new StackIntegrationEventSecuredRepository(
            new StackIntegrationEventRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class StackIntegrationEventSecuredRepository : GenericStackChildSecuredRepository<
    StackIntegrationEvent,
    StackIntegrationEventReadDto,
    StackIntegrationEventRepository,
    StackIntegrationEventCreatedEvent,
    StackIntegrationEventUpdatedEvent,
    StackIntegrationEventDeletedEvent,
    StackIntegrationEventRepositorySettings>
{
    public StackIntegrationEventSecuredRepository(
        StackIntegrationEventRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [
            OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader,
            OrganizationRole.StackContributor, OrganizationRole.StackReader,
            OrganizationRole.IntegrationContributor, OrganizationRole.IntegrationReader
        ],
        StackRoles = [StackRole.Owner, StackRole.Contributor, StackRole.Reader],
        IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor, IntegrationRole.Reader]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor, OrganizationRole.IntegrationContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor, OrganizationRole.IntegrationContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor, OrganizationRole.IntegrationContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor]
    };

    public override IQueryable<StackIntegrationEvent> ReadQuery(Guid organizationId)
    {
        var scopeQuery = base.ReadQuery(organizationId);
        var integrationRoles = ReadPermissionMap.IntegrationRoles;
        if (!integrationRoles.Any())
            return scopeQuery;

        var principalId = PrincipalProvider.GetSubject(organizationId);
        var ctx = Repository.DbContext;

        if (PrincipalDiscriminator == PrincipalDiscriminator.User)
        {
            var directIntegrationQuery =
                from e in ctx.StackIntegrationEvents
                join a in ctx.UserIntegrationRoleAssignments
                    on new { e.IntegrationId, e.OrganizationId } equals new { a.IntegrationId, a.OrganizationId }
                where e.OrganizationId == organizationId
                   && a.PrincipalId == principalId
                   && integrationRoles.Contains(a.RoleName)
                select e;

            var groupIntegrationQuery =
                from e in ctx.StackIntegrationEvents
                join gum in ctx.UserGroupMembers
                    on e.OrganizationId equals gum.OrganizationId
                join rgm in ctx.RecursiveGroupMembers
                    on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in ctx.GroupIntegrationRoleAssignments
                    on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId, IntegrationId = e.IntegrationId }
                    equals new { assignment.OrganizationId, assignment.PrincipalId, assignment.IntegrationId }
                where e.OrganizationId == organizationId
                   && gum.PrincipalId == principalId
                   && integrationRoles.Contains(assignment.RoleName)
                select e;

            return scopeQuery.Concat(directIntegrationQuery).Concat(groupIntegrationQuery);
        }

        if (PrincipalDiscriminator == PrincipalDiscriminator.ServicePrincipal)
        {
            var directIntegrationQuery =
                from e in ctx.StackIntegrationEvents
                join a in ctx.ServicePrincipalIntegrationRoleAssignments
                    on new { e.IntegrationId, e.OrganizationId } equals new { a.IntegrationId, a.OrganizationId }
                where e.OrganizationId == organizationId
                   && a.PrincipalId == principalId
                   && integrationRoles.Contains(a.RoleName)
                select e;

            var groupIntegrationQuery =
                from e in ctx.StackIntegrationEvents
                join spgm in ctx.ServicePrincipalGroupMembers
                    on e.OrganizationId equals spgm.OrganizationId
                join rgm in ctx.RecursiveGroupMembers
                    on new { RootGroupId = spgm.GroupId, RootOrganizationId = spgm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in ctx.GroupIntegrationRoleAssignments
                    on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId, IntegrationId = e.IntegrationId }
                    equals new { assignment.OrganizationId, assignment.PrincipalId, assignment.IntegrationId }
                where e.OrganizationId == organizationId
                   && spgm.PrincipalId == principalId
                   && integrationRoles.Contains(assignment.RoleName)
                select e;

            return scopeQuery.Concat(directIntegrationQuery).Concat(groupIntegrationQuery);
        }

        return scopeQuery;
    }

    public async Task<List<StackIntegrationEvent>> ListByIntegration(Guid integrationId, Guid organizationId)
    {
        return await ReadQuery(organizationId)
            .Where(e => e.IntegrationId == integrationId)
            .ToListAsync();
    }

    public async Task<List<StackIntegrationEvent>> ListByStack(Guid stackId, Guid organizationId)
    {
        return await ReadQuery(organizationId)
            .Where(e => e.StackId == stackId)
            .ToListAsync();
    }
}
