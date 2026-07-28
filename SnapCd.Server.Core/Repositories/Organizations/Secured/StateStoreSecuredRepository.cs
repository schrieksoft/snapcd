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
using SnapCd.Contracts.Dto.StateStores;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class StateStoreSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<StateStoreRepositorySettings> options)
{
    public StateStoreSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new StateStoreSecuredRepository(
            new StateStoreRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class StateStoreSecuredRepository : GenericSecuredRepository<
    StateStore,
    StateStoreReadDto,
    StateStoreRepository,
    StateStoreCreatedEvent,
    StateStoreUpdatedEvent,
    StateStoreDeletedEvent,
    StateStoreRepositorySettings>
{
    public StateStoreSecuredRepository(
        StateStoreRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader, OrganizationRole.StateStoreContributor, OrganizationRole.StateStoreReader],
        StateStoreRoles = [StateStoreRole.Owner, StateStoreRole.Contributor, StateStoreRole.Reader]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StateStoreContributor],
        StateStoreRoles = [StateStoreRole.Owner, StateStoreRole.Contributor]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StateStoreContributor, OrganizationRole.StateStoreCreator]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StateStoreContributor],
        StateStoreRoles = [StateStoreRole.Owner, StateStoreRole.Contributor]
    };

    #region overrides

    public override bool CanCreate(Guid parentId, Guid organizationId)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => CanCreateInOrganization<UserOrganizationRoleAssignment>(
                organizationId, principalId),
            PrincipalDiscriminator.ServicePrincipal => CanCreateInOrganization<ServicePrincipalOrganizationRoleAssignment>(
                organizationId, principalId),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    public override bool CanRead(Guid id, Guid organizationId)
    {
        return ReadQuery(organizationId).Any(s => s.Id == id && s.OrganizationId == organizationId);
    }

    public override bool CanUpdate(Guid id, Guid organizationId)
    {
        return UpdateQuery(organizationId).Any(s => s.Id == id && s.OrganizationId == organizationId);
    }

    public override bool CanDelete(Guid id, Guid organizationId)
    {
        return DeleteQuery(organizationId).Any(s => s.Id == id && s.OrganizationId == organizationId);
    }

    public override IQueryable<StateStore> CreateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            CreatePermissionMap.OrganizationRoles,
            CreatePermissionMap.StateStoreRoles);
    }

    public override IQueryable<StateStore> ReadQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            ReadPermissionMap.OrganizationRoles,
            ReadPermissionMap.StateStoreRoles);
    }

    public override IQueryable<StateStore> UpdateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            UpdatePermissionMap.OrganizationRoles,
            UpdatePermissionMap.StateStoreRoles);
    }

    public override IQueryable<StateStore> DeleteQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            DeletePermissionMap.OrganizationRoles,
            DeletePermissionMap.StateStoreRoles);
    }

    public override string GetParentEntityName()
    {
        return "Organization";
    }

    #endregion

    #region private

    private IQueryable<StateStore> RoleQueryDispatch(
        Guid organizationId,
        List<OrganizationRole> organizationRoles,
        List<StateStoreRole> stateStoreRoles)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => RoleQuery<
                UserOrganizationRoleAssignment,
                UserStateStoreRoleAssignment,
                UserGroupMember>(
                organizationId, principalId, organizationRoles, stateStoreRoles),
            PrincipalDiscriminator.ServicePrincipal => RoleQuery<
                ServicePrincipalOrganizationRoleAssignment,
                ServicePrincipalStateStoreRoleAssignment,
                ServicePrincipalGroupMember>(
                organizationId, principalId, organizationRoles, stateStoreRoles),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    private IQueryable<StateStore> RoleQuery<TOrganizationRoleAssignment, TStateStoreRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles,
        List<StateStoreRole> stateStoreRoles)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStateStoreRoleAssignment : class, IStateStoreRoleAssignment
        where TGroupMember : class, IGroupMember
    {
        var storesFromOrganizationRoles =
            from store in Repository.DbContext.Set<StateStore>()
            join assignment in Repository.DbContext.Set<TOrganizationRoleAssignment>()
                on store.OrganizationId equals assignment.OrganizationId
            where store.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && organizationRoles.Contains(assignment.RoleName)
            select store;

        var storesFromGroupOrganizationRoles = OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
            organizationId, principalId, organizationRoles);

        var result = storesFromOrganizationRoles
            .Concat(storesFromGroupOrganizationRoles);

        if (stateStoreRoles.Count > 0)
        {
            var storesFromStateStoreRoles =
                from store in Repository.DbContext.Set<StateStore>()
                join assignment in Repository.DbContext.Set<TStateStoreRoleAssignment>()
                    on new { StateStoreId = store.Id, store.OrganizationId } equals new { assignment.StateStoreId, assignment.OrganizationId }
                where store.OrganizationId == organizationId
                      && assignment.PrincipalId == principalId
                      && stateStoreRoles.Contains(assignment.RoleName)
                select store;

            var storesFromGroupStateStoreRoles = StateStoreRolesFromGroupQuery<TGroupMember>(
                organizationId, principalId, stateStoreRoles);

            result = result
                .Concat(storesFromStateStoreRoles)
                .Concat(storesFromGroupStateStoreRoles);
        }

        return result;
    }

    private IQueryable<StateStore> OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles)
        where TGroupMember : class, IGroupMember
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
    {
        return from store in Repository.DbContext.Set<StateStore>()
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on store.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.OrganizationId, assignment.PrincipalId }
            where store.OrganizationId == organizationId
                  && organizationRoles.Contains(assignment.RoleName)
            select store;
    }

    private IQueryable<StateStore> StateStoreRolesFromGroupQuery<TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<StateStoreRole> stateStoreRoles)
        where TGroupMember : class, IGroupMember
    {
        return from store in Repository.DbContext.Set<StateStore>()
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on store.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.Set<GroupStateStoreRoleAssignment>()
                on new { StateStoreId = store.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.StateStoreId, assignment.OrganizationId, assignment.PrincipalId }
            where store.OrganizationId == organizationId
                  && stateStoreRoles.Contains(assignment.RoleName)
            select store;
    }

    private bool CanCreateInOrganization<TOrganizationRoleAssignment>(
        Guid organizationId,
        Guid principalId)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
    {
        var hasDirectPermission = Repository.DbContext.Set<TOrganizationRoleAssignment>()
            .Any(ra => ra.OrganizationId == organizationId
                       && ra.PrincipalId == principalId
                       && CreatePermissionMap.OrganizationRoles.Contains(ra.RoleName));

        if (hasDirectPermission)
            return true;

        var hasOrgPermissionViaGroup = PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => (
                from gum in Repository.DbContext.UserGroupMembers
                where gum.UserId == principalId && gum.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                    on new { OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where CreatePermissionMap.OrganizationRoles.Contains(assignment.RoleName)
                select assignment
            ).Any(),
            PrincipalDiscriminator.ServicePrincipal => (
                from gspm in Repository.DbContext.ServicePrincipalGroupMembers
                where gspm.ServicePrincipalId == principalId && gspm.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gspm.GroupId, RootOrganizationId = gspm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                    on new { OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where CreatePermissionMap.OrganizationRoles.Contains(assignment.RoleName)
                select assignment
            ).Any(),
            _ => false
        };

        return hasOrgPermissionViaGroup;
    }

    #endregion

    #region public methods

    public async Task<StateStore> GetByName(string name, Guid organizationId)
    {
        var entity = await Repository.GetByName(name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(StateStore)} with organization ID {organizationId} and name {name} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return entity;
    }

    #endregion
}
