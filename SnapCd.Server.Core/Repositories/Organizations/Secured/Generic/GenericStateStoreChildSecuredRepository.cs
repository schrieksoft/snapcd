// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization.Base;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;

public abstract class GenericStateStoreChildSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions> :
    GenericSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TEntity : class, IEntity, IOrganizationChild, IStateStoreChild
    where TRepository : GenericStateStoreChildRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TOptions : class, IEntitySettings
{
    protected GenericStateStoreChildSecuredRepository(
        TRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader],
        StateStoreRoles = [StateStoreRole.Owner, StateStoreRole.Contributor, StateStoreRole.Reader]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor],
        StateStoreRoles = [StateStoreRole.Owner, StateStoreRole.Contributor]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor],
        StateStoreRoles = [StateStoreRole.Owner, StateStoreRole.Contributor]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor],
        StateStoreRoles = [StateStoreRole.Owner, StateStoreRole.Contributor]
    };

    public override bool CanCreate(Guid parentId, Guid organizationId)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => CanCreateForStateStore<UserOrganizationRoleAssignment, UserStateStoreRoleAssignment>(
                organizationId, principalId, parentId),
            PrincipalDiscriminator.ServicePrincipal => CanCreateForStateStore<ServicePrincipalOrganizationRoleAssignment, ServicePrincipalStateStoreRoleAssignment>(
                organizationId, principalId, parentId),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    public override bool CanRead(Guid id, Guid organizationId)
    {
        return ReadQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);
    }

    public override bool CanUpdate(Guid id, Guid organizationId)
    {
        return UpdateQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);
    }

    public override bool CanDelete(Guid id, Guid organizationId)
    {
        return DeleteQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);
    }

    public override IQueryable<TEntity> CreateQuery(Guid organizationId)
        => RoleQueryDispatch(organizationId, CreatePermissionMap);

    public override IQueryable<TEntity> ReadQuery(Guid organizationId)
        => RoleQueryDispatch(organizationId, ReadPermissionMap);

    public override IQueryable<TEntity> UpdateQuery(Guid organizationId)
        => RoleQueryDispatch(organizationId, UpdatePermissionMap);

    public override IQueryable<TEntity> DeleteQuery(Guid organizationId)
        => RoleQueryDispatch(organizationId, DeletePermissionMap);

    public override string GetParentEntityName() => "StateStore";

    protected IQueryable<TEntity> RoleQueryDispatch(Guid organizationId, PermissionMap map)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => RoleQuery<
                UserOrganizationRoleAssignment,
                UserStateStoreRoleAssignment,
                UserGroupMember>(organizationId, principalId, map.OrganizationRoles, map.StateStoreRoles),
            PrincipalDiscriminator.ServicePrincipal => RoleQuery<
                ServicePrincipalOrganizationRoleAssignment,
                ServicePrincipalStateStoreRoleAssignment,
                ServicePrincipalGroupMember>(organizationId, principalId, map.OrganizationRoles, map.StateStoreRoles),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    protected IQueryable<TEntity> RoleQuery<TOrganizationRoleAssignment, TStateStoreRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles,
        List<StateStoreRole> stateStoreRoles)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStateStoreRoleAssignment : class, IStateStoreRoleAssignment
        where TGroupMember : class, IGroupMember
    {
        IQueryable<TEntity>? combined = null;

        if (organizationRoles.Count > 0)
        {
            var direct =
                from entity in Repository.DbContext.Set<TEntity>()
                join assignment in Repository.DbContext.Set<TOrganizationRoleAssignment>()
                    on entity.OrganizationId equals assignment.OrganizationId
                where entity.OrganizationId == organizationId
                      && assignment.PrincipalId == principalId
                      && organizationRoles.Contains(assignment.RoleName)
                select entity;

            var group = OrganizationRolesFromGroupQuery<TGroupMember>(organizationId, principalId, organizationRoles);

            combined = direct.Concat(group);
        }

        if (stateStoreRoles.Count > 0)
        {
            var direct =
                from entity in Repository.DbContext.Set<TEntity>()
                join assignment in Repository.DbContext.Set<TStateStoreRoleAssignment>()
                    on new { entity.StateStoreId, entity.OrganizationId } equals new { assignment.StateStoreId, assignment.OrganizationId }
                where entity.OrganizationId == organizationId
                      && assignment.PrincipalId == principalId
                      && stateStoreRoles.Contains(assignment.RoleName)
                select entity;

            var group = StateStoreRolesFromGroupQuery<TGroupMember>(organizationId, principalId, stateStoreRoles);

            combined = combined is null ? direct.Concat(group) : combined.Concat(direct).Concat(group);
        }

        return combined ?? Repository.DbContext.Set<TEntity>().Where(_ => false);
    }

    private IQueryable<TEntity> OrganizationRolesFromGroupQuery<TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles)
        where TGroupMember : class, IGroupMember
    {
        return from entity in Repository.DbContext.Set<TEntity>()
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.OrganizationId, assignment.PrincipalId }
            where entity.OrganizationId == organizationId
                  && organizationRoles.Contains(assignment.RoleName)
            select entity;
    }

    private IQueryable<TEntity> StateStoreRolesFromGroupQuery<TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<StateStoreRole> stateStoreRoles)
        where TGroupMember : class, IGroupMember
    {
        return from entity in Repository.DbContext.Set<TEntity>()
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.Set<GroupStateStoreRoleAssignment>()
                on new { entity.StateStoreId, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.StateStoreId, assignment.OrganizationId, assignment.PrincipalId }
            where entity.OrganizationId == organizationId
                  && stateStoreRoles.Contains(assignment.RoleName)
            select entity;
    }

    protected bool CanCreateForStateStore<TOrganizationRoleAssignment, TStateStoreRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        Guid stateStoreId)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStateStoreRoleAssignment : class, IStateStoreRoleAssignment
    {
        var createOrgRoles = CreatePermissionMap.OrganizationRoles;
        var createStateStoreRoles = CreatePermissionMap.StateStoreRoles;

        if (createOrgRoles.Count > 0)
        {
            var hasDirectOrgPermission = Repository.DbContext.Set<TOrganizationRoleAssignment>()
                .Any(ra => ra.OrganizationId == organizationId
                           && ra.PrincipalId == principalId
                           && createOrgRoles.Contains(ra.RoleName));
            if (hasDirectOrgPermission) return true;

            var hasOrgPermissionViaGroup = HasOrgPermissionViaGroup(organizationId, principalId, createOrgRoles);
            if (hasOrgPermissionViaGroup) return true;
        }

        if (createStateStoreRoles.Count > 0)
        {
            var hasDirectStateStorePermission = Repository.DbContext.Set<TStateStoreRoleAssignment>()
                .Any(ra => ra.StateStoreId == stateStoreId
                           && ra.OrganizationId == organizationId
                           && ra.PrincipalId == principalId
                           && createStateStoreRoles.Contains(ra.RoleName));
            if (hasDirectStateStorePermission) return true;

            var hasStateStorePermissionViaGroup = HasStateStorePermissionViaGroup(organizationId, principalId, stateStoreId, createStateStoreRoles);
            if (hasStateStorePermissionViaGroup) return true;
        }

        return false;
    }

    private bool HasOrgPermissionViaGroup(Guid organizationId, Guid principalId, List<OrganizationRole> roles) =>
        PrincipalDiscriminator switch
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
                where roles.Contains(assignment.RoleName)
                select assignment).Any(),
            PrincipalDiscriminator.ServicePrincipal => (
                from gspm in Repository.DbContext.ServicePrincipalGroupMembers
                where gspm.ServicePrincipalId == principalId && gspm.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gspm.GroupId, RootOrganizationId = gspm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                    on new { OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where roles.Contains(assignment.RoleName)
                select assignment).Any(),
            _ => false
        };

    private bool HasStateStorePermissionViaGroup(Guid organizationId, Guid principalId, Guid stateStoreId, List<StateStoreRole> roles) =>
        PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => (
                from gum in Repository.DbContext.UserGroupMembers
                where gum.UserId == principalId && gum.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.Set<GroupStateStoreRoleAssignment>()
                    on new { StateStoreId = stateStoreId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.StateStoreId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where roles.Contains(assignment.RoleName)
                select assignment).Any(),
            PrincipalDiscriminator.ServicePrincipal => (
                from gspm in Repository.DbContext.ServicePrincipalGroupMembers
                where gspm.ServicePrincipalId == principalId && gspm.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gspm.GroupId, RootOrganizationId = gspm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.Set<GroupStateStoreRoleAssignment>()
                    on new { StateStoreId = stateStoreId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.StateStoreId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where roles.Contains(assignment.RoleName)
                select assignment).Any(),
            _ => false
        };
}
