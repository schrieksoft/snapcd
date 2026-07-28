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
using SnapCd.Contracts.Dto.Stacks;
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

public class StackSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<StackRepositorySettings> options)
{
    public StackSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new StackSecuredRepository(
            new StackRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class StackSecuredRepository : GenericSecuredRepository<
    Stack,
    StackReadDto,
    StackRepository,
    StackCreatedEvent,
    StackUpdatedEvent,
    StackDeletedEvent,
    StackRepositorySettings>
{
    public StackSecuredRepository(
        StackRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader, OrganizationRole.StackContributor, OrganizationRole.StackReader],
        StackRoles = [StackRole.Owner, StackRole.Contributor, StackRole.Reader]
    };

    public override PermissionMap ReverseInheritedReadPermissionMap => new()
    {
        NamespaceRoles = [.. Enum.GetValues<NamespaceRole>()],
        ModuleRoles = [.. Enum.GetValues<ModuleRole>()]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor, OrganizationRole.StackCreator]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor]
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

    public override IQueryable<Stack> CreateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            CreatePermissionMap.OrganizationRoles,
            [],
            false);
    }

    public override IQueryable<Stack> ReadQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            ReadPermissionMap.OrganizationRoles,
            ReadPermissionMap.StackRoles,
            true);
    }

    public override IQueryable<Stack> UpdateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            UpdatePermissionMap.OrganizationRoles,
            UpdatePermissionMap.StackRoles,
            false);
    }

    public override IQueryable<Stack> DeleteQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            DeletePermissionMap.OrganizationRoles,
            DeletePermissionMap.StackRoles,
            false);
    }

    public override string GetParentEntityName()
    {
        return "Organization";
    }

    #endregion

    #region private

    private IQueryable<Stack> RoleQueryDispatch(
        Guid organizationId,
        List<OrganizationRole> organizationRoles,
        List<StackRole> stackRoles,
        bool includeReverseInheritance)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => RoleQuery<
                UserOrganizationRoleAssignment,
                UserStackRoleAssignment,
                UserGroupMember>(
                organizationId, principalId, organizationRoles, stackRoles, includeReverseInheritance),
            PrincipalDiscriminator.ServicePrincipal => RoleQuery<
                ServicePrincipalOrganizationRoleAssignment,
                ServicePrincipalStackRoleAssignment,
                ServicePrincipalGroupMember>(
                organizationId, principalId, organizationRoles, stackRoles, includeReverseInheritance),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    private IQueryable<Stack> RoleQuery<TOrganizationRoleAssignment, TStackRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles,
        List<StackRole> stackRoles,
        bool includeReverseInheritance)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStackRoleAssignment : class, IStackRoleAssignment
        where TGroupMember : class, IGroupMember
    {
        // Branch 1: Direct org role → Stack
        var stacksFromOrganizationRoles =
            from stack in Repository.DbContext.Stacks
            join assignment in Repository.DbContext.Set<TOrganizationRoleAssignment>()
                on stack.OrganizationId equals assignment.OrganizationId
            where stack.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && organizationRoles.Contains(assignment.RoleName)
            select stack;

        // Branch 2: Group org role → Stack
        var stacksFromGroupOrganizationRoles = OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
            organizationId, principalId, organizationRoles);

        var result = stacksFromOrganizationRoles
            .Concat(stacksFromGroupOrganizationRoles);

        if (stackRoles.Count > 0)
        {
            // Branch 3: Direct stack role → Stack
            var stacksFromStackRoles =
                from stack in Repository.DbContext.Stacks
                join assignment in Repository.DbContext.Set<TStackRoleAssignment>()
                    on new { StackId = stack.Id, stack.OrganizationId } equals new { assignment.StackId, assignment.OrganizationId }
                where stack.OrganizationId == organizationId
                      && assignment.PrincipalId == principalId
                      && stackRoles.Contains(assignment.RoleName)
                select stack;

            // Branch 4: Group stack role → Stack
            var stacksFromGroupStackRoles = StackRolesFromGroupQuery<TGroupMember, TStackRoleAssignment>(
                organizationId, principalId, stackRoles);

            result = result
                .Concat(stacksFromStackRoles)
                .Concat(stacksFromGroupStackRoles);
        }

        if (includeReverseInheritance)
        {
            result = result.Concat(
                ReverseInheritanceQuery<TGroupMember>(organizationId, principalId));
        }

        return result;
    }

    private IQueryable<Stack> OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles)
        where TGroupMember : class, IGroupMember
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
    {
        return from stack in Repository.DbContext.Stacks
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on stack.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.OrganizationId, assignment.PrincipalId }
            where stack.OrganizationId == organizationId
                  && organizationRoles.Contains(assignment.RoleName)
            select stack;
    }

    private IQueryable<Stack> StackRolesFromGroupQuery<TGroupMember, TStackRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<StackRole> stackRoles)
        where TGroupMember : class, IGroupMember
        where TStackRoleAssignment : class, IStackRoleAssignment
    {
        return from stack in Repository.DbContext.Stacks
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on stack.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupStackRoleAssignments
                on new { StackId = stack.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.StackId, assignment.OrganizationId, assignment.PrincipalId }
            where stack.OrganizationId == organizationId
                  && stackRoles.Contains(assignment.RoleName)
            select stack;
    }

    // Any role on a contained Namespace or Module suffices, so these queries do not
    // filter on role names (see ReverseInheritedReadPermissionMap).
    private IQueryable<Stack> ReverseInheritanceQuery<TGroupMember>(
        Guid organizationId,
        Guid principalId)
        where TGroupMember : class, IGroupMember
    {
        return ReverseInheritanceDirectNamespaceRoleQuery(organizationId, principalId)
            .Concat(ReverseInheritanceGroupNamespaceRoleQuery<TGroupMember>(organizationId, principalId))
            .Concat(ReverseInheritanceDirectModuleRoleQuery(organizationId, principalId))
            .Concat(ReverseInheritanceGroupModuleRoleQuery<TGroupMember>(organizationId, principalId));
    }

    private IQueryable<Stack> ReverseInheritanceDirectNamespaceRoleQuery(
        Guid organizationId,
        Guid principalId)
    {
        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => ReverseInheritanceDirectNamespaceRoleQuery<UserNamespaceRoleAssignment>(organizationId, principalId),
            PrincipalDiscriminator.ServicePrincipal => ReverseInheritanceDirectNamespaceRoleQuery<ServicePrincipalNamespaceRoleAssignment>(organizationId, principalId),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    private IQueryable<Stack> ReverseInheritanceDirectNamespaceRoleQuery<TNamespaceRoleAssignment>(
        Guid organizationId,
        Guid principalId)
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
    {
        return from assignment in Repository.DbContext.Set<TNamespaceRoleAssignment>()
            where assignment.PrincipalId == principalId
                  && assignment.OrganizationId == organizationId
            join ns in Repository.DbContext.Namespaces
                on new { assignment.NamespaceId, assignment.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join stack in Repository.DbContext.Stacks
                on new { ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            select stack;
    }

    private IQueryable<Stack> ReverseInheritanceGroupNamespaceRoleQuery<TGroupMember>(
        Guid organizationId,
        Guid principalId)
        where TGroupMember : class, IGroupMember
    {
        return from groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupNamespaceRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.OrganizationId, assignment.PrincipalId }
            join ns in Repository.DbContext.Namespaces
                on new { assignment.NamespaceId, assignment.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join stack in Repository.DbContext.Stacks
                on new { ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            select stack;
    }

    private IQueryable<Stack> ReverseInheritanceDirectModuleRoleQuery(
        Guid organizationId,
        Guid principalId)
    {
        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => ReverseInheritanceDirectModuleRoleQuery<UserModuleRoleAssignment>(organizationId, principalId),
            PrincipalDiscriminator.ServicePrincipal => ReverseInheritanceDirectModuleRoleQuery<ServicePrincipalModuleRoleAssignment>(organizationId, principalId),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    private IQueryable<Stack> ReverseInheritanceDirectModuleRoleQuery<TModuleRoleAssignment>(
        Guid organizationId,
        Guid principalId)
        where TModuleRoleAssignment : class, IModuleRoleAssignment
    {
        return from assignment in Repository.DbContext.Set<TModuleRoleAssignment>()
            where assignment.PrincipalId == principalId
                  && assignment.OrganizationId == organizationId
            join module in Repository.DbContext.Modules
                on new { assignment.ModuleId, assignment.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join stack in Repository.DbContext.Stacks
                on new { ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            select stack;
    }

    private IQueryable<Stack> ReverseInheritanceGroupModuleRoleQuery<TGroupMember>(
        Guid organizationId,
        Guid principalId)
        where TGroupMember : class, IGroupMember
    {
        return from groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupModuleRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.OrganizationId, assignment.PrincipalId }
            join module in Repository.DbContext.Modules
                on new { assignment.ModuleId, assignment.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join stack in Repository.DbContext.Stacks
                on new { ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            select stack;
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

    public async Task<Stack> GetByName(string name, Guid organizationId)
    {
        var entity = await Repository.GetByName(name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(Stack)} with organization ID {organizationId} and name {name} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return entity;
    }

    #endregion
}
