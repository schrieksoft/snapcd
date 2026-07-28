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
using SnapCd.Contracts.Dto.Integrations;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

/// <summary>
/// Secures the Integration entity at org-role level (Owner/Contributor/Reader), mirroring
/// <c>AgentSecuredRepository</c>. Per-integration roles gate the integration's *child* resources (its role
/// assignments), not the entity row itself.
/// </summary>
public class IntegrationSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<IntegrationRepositorySettings> options)
{
    public IntegrationSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        principalProvider ??= new HttpContextPrincipalProvider(new HttpContextAccessor());
        return new IntegrationSecuredRepository(
            new IntegrationRepository(dbFactory.CreateDbContext(), principalProvider, bus, options), principalProvider);
    }
}

public class IntegrationSecuredRepository(IntegrationRepository repository, IPrincipalProvider principalProvider)
    : GenericOrganizationChildSecuredRepository<Integration, IntegrationReadDto, IntegrationRepository,
        IntegrationCreatedEvent, IntegrationUpdatedEvent, IntegrationDeletedEvent, IntegrationRepositorySettings>(repository, principalProvider)
{
    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader, OrganizationRole.IntegrationContributor, OrganizationRole.IntegrationReader],
        IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor, IntegrationRole.Reader]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.IntegrationContributor],
        IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.IntegrationContributor, OrganizationRole.IntegrationCreator]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.IntegrationContributor],
        IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor]
    };

    public override IQueryable<Integration> ReadQuery(Guid organizationId)
        => base.ReadQuery(organizationId).Concat(IntegrationRoleQuery(organizationId, ReadPermissionMap.IntegrationRoles));

    public override IQueryable<Integration> UpdateQuery(Guid organizationId)
        => base.UpdateQuery(organizationId).Concat(IntegrationRoleQuery(organizationId, UpdatePermissionMap.IntegrationRoles));

    public override IQueryable<Integration> DeleteQuery(Guid organizationId)
        => base.DeleteQuery(organizationId).Concat(IntegrationRoleQuery(organizationId, DeletePermissionMap.IntegrationRoles));

    private IQueryable<Integration> IntegrationRoleQuery(Guid organizationId, List<IntegrationRole> roles)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => IntegrationRoleQuery<UserIntegrationRoleAssignment, UserGroupMember>(organizationId, principalId, roles),
            PrincipalDiscriminator.ServicePrincipal => IntegrationRoleQuery<ServicePrincipalIntegrationRoleAssignment, ServicePrincipalGroupMember>(organizationId, principalId, roles),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    private IQueryable<Integration> IntegrationRoleQuery<TRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<IntegrationRole> roles)
        where TRoleAssignment : class, IIntegrationRoleAssignment
        where TGroupMember : class, IGroupMember
    {
        var direct =
            from entity in Repository.DbContext.Integrations
            join assignment in Repository.DbContext.Set<TRoleAssignment>()
                on new { IntegrationId = entity.Id, entity.OrganizationId } equals new { assignment.IntegrationId, assignment.OrganizationId }
            where entity.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && roles.Contains(assignment.RoleName)
            select entity;

        var group =
            from entity in Repository.DbContext.Integrations
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupIntegrationRoleAssignments
                on new { IntegrationId = entity.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.IntegrationId, assignment.OrganizationId, assignment.PrincipalId }
            where entity.OrganizationId == organizationId
                  && roles.Contains(assignment.RoleName)
            select entity;

        return direct.Concat(group);
    }

}
