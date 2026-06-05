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

/// <summary>
/// Secured-repo base for entities that are children of a Runner (have a <c>RunnerId</c> column).
/// Read paths consume both <c>OrganizationRoles</c> and <c>RunnerRoles</c>; write paths consume
/// <c>RunnerRoles</c> only — org-level Owners do NOT automatically write these by default. Override
/// the relevant PermissionMap to broaden the rule.
/// </summary>
public abstract class GenericRunnerChildSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions> :
    GenericSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TEntity : class, IEntity, IOrganizationChild, IRunnerChild
    where TRepository : GenericOrganizationChildRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TOptions : class, IEntitySettings
{
    protected GenericRunnerChildSecuredRepository(
        TRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader, OrganizationRole.RunnerContributor, OrganizationRole.RunnerReader],
        RunnerRoles = [RunnerRole.Owner, RunnerRole.Contributor, RunnerRole.Reader]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.RunnerContributor],
        RunnerRoles = [RunnerRole.Owner, RunnerRole.Contributor]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.RunnerContributor],
        RunnerRoles = [RunnerRole.Owner, RunnerRole.Contributor]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.RunnerContributor],
        RunnerRoles = [RunnerRole.Owner, RunnerRole.Contributor]
    };

    public override bool CanCreate(Guid parentId, Guid organizationId)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => CanCreateForRunner<UserOrganizationRoleAssignment, UserRunnerRoleAssignment>(
                organizationId, principalId, parentId),
            PrincipalDiscriminator.ServicePrincipal => CanCreateForRunner<ServicePrincipalOrganizationRoleAssignment, ServicePrincipalRunnerRoleAssignment>(
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

    public override string GetParentEntityName() => "Runner";

    protected IQueryable<TEntity> RoleQueryDispatch(Guid organizationId, PermissionMap map)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => RoleQuery<
                UserOrganizationRoleAssignment,
                UserRunnerRoleAssignment,
                UserGroupMember>(organizationId, principalId, map.OrganizationRoles, map.RunnerRoles),
            PrincipalDiscriminator.ServicePrincipal => RoleQuery<
                ServicePrincipalOrganizationRoleAssignment,
                ServicePrincipalRunnerRoleAssignment,
                ServicePrincipalGroupMember>(organizationId, principalId, map.OrganizationRoles, map.RunnerRoles),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    protected IQueryable<TEntity> RoleQuery<TOrganizationRoleAssignment, TRunnerRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles,
        List<RunnerRole> runnerRoles)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TRunnerRoleAssignment : class, IRunnerRoleAssignment
        where TGroupMember : class, IGroupMember
    {
        // Start with an empty queryable; concat in only the role sources that have roles to check.
        // Skipping empty lists avoids the SQL cost of joins whose IN-clause is empty.
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

        if (runnerRoles.Count > 0)
        {
            var direct =
                from entity in Repository.DbContext.Set<TEntity>()
                join assignment in Repository.DbContext.Set<TRunnerRoleAssignment>()
                    on new { entity.RunnerId, entity.OrganizationId } equals new { assignment.RunnerId, assignment.OrganizationId }
                where entity.OrganizationId == organizationId
                      && assignment.PrincipalId == principalId
                      && runnerRoles.Contains(assignment.RoleName)
                select entity;

            var group = RunnerRolesFromGroupQuery<TGroupMember>(organizationId, principalId, runnerRoles);

            combined = combined is null ? direct.Concat(group) : combined.Concat(direct).Concat(group);
        }

        // If both lists are empty, no rows are permitted — return an empty queryable.
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

    private IQueryable<TEntity> RunnerRolesFromGroupQuery<TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<RunnerRole> runnerRoles)
        where TGroupMember : class, IGroupMember
    {
        return from entity in Repository.DbContext.Set<TEntity>()
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupRunnerRoleAssignments
                on new { entity.RunnerId, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.RunnerId, assignment.OrganizationId, assignment.PrincipalId }
            where entity.OrganizationId == organizationId
                  && groupMember.PrincipalId == principalId
                  && runnerRoles.Contains(assignment.RoleName)
            select entity;
    }

    protected bool CanCreateForRunner<TOrganizationRoleAssignment, TRunnerRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        Guid runnerId)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TRunnerRoleAssignment : class, IRunnerRoleAssignment
    {
        var createOrgRoles = CreatePermissionMap.OrganizationRoles;
        var createRunnerRoles = CreatePermissionMap.RunnerRoles;

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

        if (createRunnerRoles.Count > 0)
        {
            var hasDirectRunnerPermission = Repository.DbContext.Set<TRunnerRoleAssignment>()
                .Any(ra => ra.RunnerId == runnerId
                           && ra.OrganizationId == organizationId
                           && ra.PrincipalId == principalId
                           && createRunnerRoles.Contains(ra.RoleName));
            if (hasDirectRunnerPermission) return true;

            var hasRunnerPermissionViaGroup = HasRunnerPermissionViaGroup(organizationId, principalId, runnerId, createRunnerRoles);
            if (hasRunnerPermissionViaGroup) return true;
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

    private bool HasRunnerPermissionViaGroup(Guid organizationId, Guid principalId, Guid runnerId, List<RunnerRole> roles) =>
        PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => (
                from gum in Repository.DbContext.UserGroupMembers
                where gum.UserId == principalId && gum.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupRunnerRoleAssignments
                    on new { RunnerId = runnerId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.RunnerId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where roles.Contains(assignment.RoleName)
                select assignment).Any(),
            PrincipalDiscriminator.ServicePrincipal => (
                from gspm in Repository.DbContext.ServicePrincipalGroupMembers
                where gspm.ServicePrincipalId == principalId && gspm.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gspm.GroupId, RootOrganizationId = gspm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupRunnerRoleAssignments
                    on new { RunnerId = runnerId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.RunnerId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where roles.Contains(assignment.RoleName)
                select assignment).Any(),
            _ => false
        };
}
