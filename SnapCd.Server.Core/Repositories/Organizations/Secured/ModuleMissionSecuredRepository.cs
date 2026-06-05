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
using SnapCd.Contracts.Dto.Missions;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Missions;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ModuleMissionSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModuleMissionRepositorySettings> options)
{
    public ModuleMissionSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleMissionSecuredRepository(
            new ModuleMissionRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ModuleMissionSecuredRepository : GenericModuleChildSecuredRepository<
    ModuleMission,
    ModuleMissionReadDto,
    ModuleMissionRepository,
    ModuleMissionCreatedEvent,
    ModuleMissionUpdatedEvent,
    ModuleMissionDeletedEvent,
    ModuleMissionRepositorySettings>
{
    public ModuleMissionSecuredRepository(
        ModuleMissionRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [
            OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader,
            OrganizationRole.StackContributor, OrganizationRole.StackReader,
            OrganizationRole.AgentContributor, OrganizationRole.AgentReader
        ],
        StackRoles = [StackRole.Owner, StackRole.Contributor, StackRole.Reader],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor, NamespaceRole.Reader],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.Contributor, ModuleRole.Reader],
        AgentRoles = [AgentRole.Owner, AgentRole.Contributor, AgentRole.Reader]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor, OrganizationRole.AgentContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.Contributor]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor, OrganizationRole.AgentContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.Contributor]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor, OrganizationRole.AgentContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.Contributor]
    };

    public override IQueryable<ModuleMission> ReadQuery(Guid organizationId)
    {
        var scopeQuery = base.ReadQuery(organizationId);
        var agentRoles = ReadPermissionMap.AgentRoles;
        if (!agentRoles.Any())
            return scopeQuery;

        var principalId = PrincipalProvider.GetSubject(organizationId);
        var ctx = Repository.DbContext;

        if (PrincipalDiscriminator == PrincipalDiscriminator.User)
        {
            var directAgentQuery =
                from m in ctx.ModuleMissions
                join a in ctx.UserAgentRoleAssignments
                    on new { m.AgentId, m.OrganizationId } equals new { a.AgentId, a.OrganizationId }
                where m.OrganizationId == organizationId
                   && a.PrincipalId == principalId
                   && agentRoles.Contains(a.RoleName)
                select m;

            var groupAgentQuery =
                from m in ctx.ModuleMissions
                join gum in ctx.UserGroupMembers
                    on m.OrganizationId equals gum.OrganizationId
                join rgm in ctx.RecursiveGroupMembers
                    on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in ctx.GroupAgentRoleAssignments
                    on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId, AgentId = m.AgentId }
                    equals new { assignment.OrganizationId, assignment.PrincipalId, assignment.AgentId }
                where m.OrganizationId == organizationId
                   && gum.PrincipalId == principalId
                   && agentRoles.Contains(assignment.RoleName)
                select m;

            return scopeQuery.Concat(directAgentQuery).Concat(groupAgentQuery);
        }

        if (PrincipalDiscriminator == PrincipalDiscriminator.ServicePrincipal)
        {
            var directAgentQuery =
                from m in ctx.ModuleMissions
                join a in ctx.ServicePrincipalAgentRoleAssignments
                    on new { m.AgentId, m.OrganizationId } equals new { a.AgentId, a.OrganizationId }
                where m.OrganizationId == organizationId
                   && a.PrincipalId == principalId
                   && agentRoles.Contains(a.RoleName)
                select m;

            var groupAgentQuery =
                from m in ctx.ModuleMissions
                join spgm in ctx.ServicePrincipalGroupMembers
                    on m.OrganizationId equals spgm.OrganizationId
                join rgm in ctx.RecursiveGroupMembers
                    on new { RootGroupId = spgm.GroupId, RootOrganizationId = spgm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in ctx.GroupAgentRoleAssignments
                    on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId, AgentId = m.AgentId }
                    equals new { assignment.OrganizationId, assignment.PrincipalId, assignment.AgentId }
                where m.OrganizationId == organizationId
                   && spgm.PrincipalId == principalId
                   && agentRoles.Contains(assignment.RoleName)
                select m;

            return scopeQuery.Concat(directAgentQuery).Concat(groupAgentQuery);
        }

        return scopeQuery;
    }

    public async Task<List<ModuleMission>> ListByAgent(Guid agentId, Guid organizationId)
    {
        return await ReadQuery(organizationId)
            .Where(m => m.AgentId == agentId)
            .ToListAsync();
    }

    public async Task<List<ModuleMission>> ListByModule(Guid moduleId, Guid organizationId)
    {
        return await ReadQuery(organizationId)
            .Where(m => m.ModuleId == moduleId)
            .ToListAsync();
    }
}
