// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization.Base;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;

/// <summary>
/// Secured-repo base for entities that are children of an Integration (have an <c>IntegrationId</c> column).
/// Read paths consume both <c>OrganizationRoles</c> and <c>IntegrationRoles</c>; write paths consume both
/// too. Mirrors <see cref="GenericAgentChildSecuredRepository{TEntity,TDto,TRepository,TCreateEvent,TUpdateEvent,TDeleteEvent,TOptions}"/>.
/// </summary>
public abstract class GenericIntegrationChildSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions> :
    GenericSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TEntity : class, IEntity, IOrganizationChild, IIntegrationChild
    where TRepository : GenericOrganizationChildRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TOptions : class, IEntitySettings
{
    protected GenericIntegrationChildSecuredRepository(
        TRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader, OrganizationRole.IntegrationContributor, OrganizationRole.IntegrationReader],
        IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor, IntegrationRole.Reader]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IntegrationContributor],
        IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IntegrationContributor],
        IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IntegrationContributor],
        IntegrationRoles = [IntegrationRole.Owner, IntegrationRole.Contributor]
    };

    public override bool CanCreate(Guid parentId, Guid organizationId)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => CanCreateForIntegration<UserOrganizationRoleAssignment, UserIntegrationRoleAssignment>(
                organizationId, principalId, parentId),
            PrincipalDiscriminator.ServicePrincipal => CanCreateForIntegration<ServicePrincipalOrganizationRoleAssignment, ServicePrincipalIntegrationRoleAssignment>(
                organizationId, principalId, parentId),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    public override bool CanRead(Guid id, Guid organizationId)
        => ReadQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);

    public override bool CanUpdate(Guid id, Guid organizationId)
        => UpdateQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);

    public override bool CanDelete(Guid id, Guid organizationId)
        => DeleteQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);

    public override IQueryable<TEntity> CreateQuery(Guid organizationId) => RoleQueryDispatch(organizationId, CreatePermissionMap);
    public override IQueryable<TEntity> ReadQuery(Guid organizationId) => RoleQueryDispatch(organizationId, ReadPermissionMap);
    public override IQueryable<TEntity> UpdateQuery(Guid organizationId) => RoleQueryDispatch(organizationId, UpdatePermissionMap);
    public override IQueryable<TEntity> DeleteQuery(Guid organizationId) => RoleQueryDispatch(organizationId, DeletePermissionMap);

    public override string GetParentEntityName() => "Integration";

    protected IQueryable<TEntity> RoleQueryDispatch(Guid organizationId, PermissionMap map)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => RoleQuery<
                UserOrganizationRoleAssignment,
                UserIntegrationRoleAssignment,
                Entities.Definition.GroupMembers.UserGroupMember>(organizationId, principalId, map.OrganizationRoles, map.IntegrationRoles),
            PrincipalDiscriminator.ServicePrincipal => RoleQuery<
                ServicePrincipalOrganizationRoleAssignment,
                ServicePrincipalIntegrationRoleAssignment,
                Entities.Definition.GroupMembers.ServicePrincipalGroupMember>(organizationId, principalId, map.OrganizationRoles, map.IntegrationRoles),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    protected IQueryable<TEntity> RoleQuery<TOrganizationRoleAssignment, TIntegrationRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles,
        List<IntegrationRole> integrationRoles)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TIntegrationRoleAssignment : class, IIntegrationRoleAssignment
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

        if (integrationRoles.Count > 0)
        {
            var direct =
                from entity in Repository.DbContext.Set<TEntity>()
                join assignment in Repository.DbContext.Set<TIntegrationRoleAssignment>()
                    on new { entity.IntegrationId, entity.OrganizationId } equals new { assignment.IntegrationId, assignment.OrganizationId }
                where entity.OrganizationId == organizationId
                      && assignment.PrincipalId == principalId
                      && integrationRoles.Contains(assignment.RoleName)
                select entity;

            var group = IntegrationRolesFromGroupQuery<TGroupMember>(organizationId, principalId, integrationRoles);

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
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.OrganizationId, assignment.PrincipalId }
            where entity.OrganizationId == organizationId
                  && groupMember.PrincipalId == principalId
                  && organizationRoles.Contains(assignment.RoleName)
            select entity;
    }

    private IQueryable<TEntity> IntegrationRolesFromGroupQuery<TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<IntegrationRole> integrationRoles)
        where TGroupMember : class, IGroupMember
    {
        return from entity in Repository.DbContext.Set<TEntity>()
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupIntegrationRoleAssignments
                on new { entity.IntegrationId, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.IntegrationId, assignment.OrganizationId, assignment.PrincipalId }
            where entity.OrganizationId == organizationId
                  && groupMember.PrincipalId == principalId
                  && integrationRoles.Contains(assignment.RoleName)
            select entity;
    }

    protected bool CanCreateForIntegration<TOrganizationRoleAssignment, TIntegrationRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        Guid integrationId)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TIntegrationRoleAssignment : class, IIntegrationRoleAssignment
    {
        var createOrgRoles = CreatePermissionMap.OrganizationRoles;
        var createIntegrationRoles = CreatePermissionMap.IntegrationRoles;

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

        if (createIntegrationRoles.Count > 0)
        {
            var hasDirectIntegrationPermission = Repository.DbContext.Set<TIntegrationRoleAssignment>()
                .Any(ra => ra.IntegrationId == integrationId
                           && ra.OrganizationId == organizationId
                           && ra.PrincipalId == principalId
                           && createIntegrationRoles.Contains(ra.RoleName));
            if (hasDirectIntegrationPermission) return true;

            var hasIntegrationPermissionViaGroup = HasIntegrationPermissionViaGroup(organizationId, principalId, integrationId, createIntegrationRoles);
            if (hasIntegrationPermissionViaGroup) return true;
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

    private bool HasIntegrationPermissionViaGroup(Guid organizationId, Guid principalId, Guid integrationId, List<IntegrationRole> roles) =>
        PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => (
                from gum in Repository.DbContext.UserGroupMembers
                where gum.UserId == principalId && gum.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupIntegrationRoleAssignments
                    on new { IntegrationId = integrationId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.IntegrationId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where roles.Contains(assignment.RoleName)
                select assignment).Any(),
            PrincipalDiscriminator.ServicePrincipal => (
                from gspm in Repository.DbContext.ServicePrincipalGroupMembers
                where gspm.ServicePrincipalId == principalId && gspm.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gspm.GroupId, RootOrganizationId = gspm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupIntegrationRoleAssignments
                    on new { IntegrationId = integrationId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.IntegrationId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where roles.Contains(assignment.RoleName)
                select assignment).Any(),
            _ => false
        };
}
