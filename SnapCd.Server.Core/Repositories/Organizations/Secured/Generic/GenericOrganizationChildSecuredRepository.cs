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

public abstract class GenericOrganizationChildSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions> :
    GenericSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TEntity : class, IEntity, IOrganizationChild
    where TRepository : GenericOrganizationChildRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TOptions : class, IEntitySettings
{
    protected GenericOrganizationChildSecuredRepository(
        TRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader] };
    public override PermissionMap UpdatePermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor] };
    public override PermissionMap CreatePermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor] };
    public override PermissionMap DeletePermissionMap => new() { OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor] };

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
    {
        return RoleQueryDispatch(
            organizationId,
            CreatePermissionMap.OrganizationRoles);
    }

    public override IQueryable<TEntity> ReadQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            ReadPermissionMap.OrganizationRoles);
    }

    public override IQueryable<TEntity> UpdateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            UpdatePermissionMap.OrganizationRoles);
    }

    public override IQueryable<TEntity> DeleteQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            DeletePermissionMap.OrganizationRoles);
    }

    public override string GetParentEntityName()
    {
        return "Organization";
    }

    protected IQueryable<TEntity> RoleQueryDispatch(
        Guid organizationId,
        List<OrganizationRole> organizationRoles)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => RoleQuery<UserOrganizationRoleAssignment, UserGroupMember>(
                organizationId, principalId, organizationRoles),
            PrincipalDiscriminator.ServicePrincipal => RoleQuery<ServicePrincipalOrganizationRoleAssignment, ServicePrincipalGroupMember>(
                organizationId, principalId, organizationRoles),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    protected IQueryable<TEntity> RoleQuery<TOrganizationRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TGroupMember : class, IGroupMember
    {
        // Direct role assignment on Organization
        var entitiesFromDirectRoles = from entity in Repository.DbContext.Set<TEntity>()
            join assignment in Repository.DbContext.Set<TOrganizationRoleAssignment>()
                on entity.OrganizationId equals assignment.OrganizationId
            where entity.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && organizationRoles.Contains(assignment.RoleName)
            select entity;

        // Group-based role assignment on Organization
        var entitiesFromGroupRoles = OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
            organizationId, principalId, organizationRoles);

        return entitiesFromDirectRoles
            .Concat(entitiesFromGroupRoles);
    }

    private IQueryable<TEntity> OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles)
        where TGroupMember : class, IGroupMember
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
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

    protected bool CanCreateInOrganization<TOrganizationRoleAssignment>(
        Guid organizationId,
        Guid principalId)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
    {
        // Check direct role assignment
        var hasDirectPermission = Repository.DbContext.Set<TOrganizationRoleAssignment>()
            .Any(ra => ra.OrganizationId == organizationId
                       && ra.PrincipalId == principalId
                       && (ra.RoleName == OrganizationRole.Owner || ra.RoleName == OrganizationRole.Contributor));

        if (hasDirectPermission)
            return true;

        // Check group-based organization role assignment
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
                where assignment.RoleName == OrganizationRole.Owner || assignment.RoleName == OrganizationRole.Contributor
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
                where assignment.RoleName == OrganizationRole.Owner || assignment.RoleName == OrganizationRole.Contributor
                select assignment
            ).Any(),
            _ => false
        };

        return hasOrgPermissionViaGroup;
    }
}