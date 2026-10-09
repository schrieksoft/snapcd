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
using SnapCd.Contracts.Dto.Namespaces;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Views;
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
using System.Linq.Expressions;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class NamespaceSecuredRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<NamespaceRepositorySettings> options)
{
    public NamespaceSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceSecuredRepository(new NamespaceRepository(dbContext, principalProvider, bus, options), principalProvider);
    }
}

public class NamespaceSecuredRepository : GenericSecuredRepository<Namespace, NamespaceReadDto, NamespaceMetadata, NamespaceRepository, NamespaceCreatedEvent, NamespaceUpdatedEvent, NamespaceDeletedEvent,
    NamespaceRepositorySettings>
{
    public NamespaceSecuredRepository(NamespaceRepository namespaceRepository, IPrincipalProvider principalProvider) : base(namespaceRepository, principalProvider)
    {
    }

    #region overrides

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor, StackRole.NamespaceCreator]
    };

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader, OrganizationRole.StackContributor, OrganizationRole.StackReader],
        StackRoles = [StackRole.Owner, StackRole.Contributor, StackRole.Reader],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor, NamespaceRole.Reader]
    };

    /// <summary>
    /// Any role on a contained Module lets the principal discover this Namespace's name and id, so
    /// they can navigate to the Module they do have access to. The Namespace's own defaults stay
    /// behind read.
    /// </summary>
    public override PermissionMap ReverseInheritedReadMetadataPermissionMap => new()
    {
        ModuleRoles = [.. Enum.GetValues<ModuleRole>()]
    };

    public override PermissionMap ReadMetadataPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader, OrganizationRole.StackContributor, OrganizationRole.StackReader, OrganizationRole.StackMetadataReader],
        StackRoles = [StackRole.Owner, StackRole.Contributor, StackRole.Reader, StackRole.MetadataReader],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor, NamespaceRole.Reader, NamespaceRole.MetadataReader]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner]
    };

    public override bool CanCreate(Guid parentId, Guid organizationId)
    {
        // Check if the user has permission to create namespaces in the given stack (parentId = stackId)
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => CanCreateInStack<UserOrganizationRoleAssignment, UserStackRoleAssignment>(
                organizationId, principalId, parentId),
            PrincipalDiscriminator.ServicePrincipal => CanCreateInStack<ServicePrincipalOrganizationRoleAssignment, ServicePrincipalStackRoleAssignment>(
                organizationId, principalId, parentId),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    public override bool CanRead(Guid id, Guid organizationId)
    {
        return ReadQuery(organizationId).Any(n => n.Id == id && n.OrganizationId == organizationId);
    }

    public override bool CanUpdate(Guid id, Guid organizationId)
    {
        return UpdateQuery(organizationId).Any(n => n.Id == id && n.OrganizationId == organizationId);
    }

    public override bool CanDelete(Guid id, Guid organizationId)
    {
        return DeleteQuery(organizationId).Any(n => n.Id == id && n.OrganizationId == organizationId);
    }

    public override IQueryable<Namespace> CreateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            CreatePermissionMap.OrganizationRoles,
            CreatePermissionMap.StackRoles,
            CreatePermissionMap.NamespaceRoles
        );
    }

    public override IQueryable<Namespace> ReadQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            ReadPermissionMap.OrganizationRoles,
            ReadPermissionMap.StackRoles,
            ReadPermissionMap.NamespaceRoles
        );
    }


    /// <summary>
    /// The scope with the names above it, for the callers that must tell two same-named rows
    /// apart. Scoped by metadata read, as the plain metadata list is.
    /// </summary>
    /// <summary>One scope with the names above it, once metadata read for it is established.</summary>
    public async Task<QualifiedNamespace> GetQualifiedNamespace(Guid id, Guid organizationId)
    {
        if (!CanReadMetadata(id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Namespace with ID {id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read its metadata.");

        return await Repository.GetQualifiedNamespace(id, organizationId);
    }

    public Task<List<QualifiedNamespace>> ListQualifiedNamespace(
        Guid organizationId,
        Func<IQueryable<Namespace>, IQueryable<Namespace>>? queryModifier = null,
        Func<IQueryable<QualifiedNamespace>, IOrderedQueryable<QualifiedNamespace>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
        => Repository.ListQualifiedNamespace(
            organizationId, ReadMetadataQuery(organizationId), queryModifier, orderBy, pageNumber, pageSize);

    public override IQueryable<Namespace> ReadMetadataQuery(Guid organizationId)
    {
        var baseQuery = RoleQueryDispatch(
            organizationId,
            ReadMetadataPermissionMap.OrganizationRoles,
            ReadMetadataPermissionMap.StackRoles,
            ReadMetadataPermissionMap.NamespaceRoles
        );

        var reverseInheritanceQuery = ReverseInheritanceQuery(organizationId);

        if (reverseInheritanceQuery != null)
            baseQuery = baseQuery.Concat(reverseInheritanceQuery);

        return baseQuery;
    }

    public override IQueryable<Namespace> UpdateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            UpdatePermissionMap.OrganizationRoles,
            UpdatePermissionMap.StackRoles,
            UpdatePermissionMap.NamespaceRoles
        );
    }

    public override IQueryable<Namespace> DeleteQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            DeletePermissionMap.OrganizationRoles,
            DeletePermissionMap.StackRoles,
            DeletePermissionMap.NamespaceRoles
        );
    }

    public override string GetParentEntityName()
    {
        return "Stack";
    }

    #endregion

    #region private

    private IQueryable<Namespace> RoleQueryDispatch(
        Guid organizationId,
        List<OrganizationRole> organizationRoles,
        List<StackRole> stackRoles,
        List<NamespaceRole> namespaceRoles)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => RoleQuery<
                UserOrganizationRoleAssignment,
                UserStackRoleAssignment,
                UserNamespaceRoleAssignment,
                UserGroupMember>(
                organizationId, principalId, organizationRoles, stackRoles, namespaceRoles),
            PrincipalDiscriminator.ServicePrincipal => RoleQuery<
                ServicePrincipalOrganizationRoleAssignment,
                ServicePrincipalStackRoleAssignment,
                ServicePrincipalNamespaceRoleAssignment,
                ServicePrincipalGroupMember>(
                organizationId, principalId, organizationRoles, stackRoles, namespaceRoles),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    private IQueryable<Namespace> RoleQuery<TOrganizationRoleAssignment, TStackRoleAssignment, TNamespaceRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles,
        List<StackRole> stackRoles,
        List<NamespaceRole> namespaceRoles)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStackRoleAssignment : class, IStackRoleAssignment
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
        where TGroupMember : class, IGroupMember
    {
        // Direct role assignments
        var namespacesFromNamespaceRoles =
            from ns in Repository.DbContext.Namespaces
            join assignment in Repository.DbContext.Set<TNamespaceRoleAssignment>()
                on new { NamespaceId = ns.Id, ns.OrganizationId } equals new { assignment.NamespaceId, assignment.OrganizationId }
            where ns.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && namespaceRoles.Contains(assignment.RoleName)
            select ns;

        var namespacesFromStackRoles =
            from ns in Repository.DbContext.Namespaces
            join stack in Repository.DbContext.Stacks
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join assignment in Repository.DbContext.Set<TStackRoleAssignment>()
                on new { StackId = stack.Id, stack.OrganizationId } equals new { assignment.StackId, assignment.OrganizationId }
            where ns.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && stackRoles.Contains(assignment.RoleName)
            select ns;

        var namespacesFromOrganizationRoles =
            from ns in Repository.DbContext.Namespaces
            join assignment in Repository.DbContext.Set<TOrganizationRoleAssignment>()
                on ns.OrganizationId equals assignment.OrganizationId
            where ns.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && organizationRoles.Contains(assignment.RoleName)
            select ns;

        // Group-based role assignments
        var namespacesFromGroupNamespaceRoles = NamespaceRolesFromGroupQuery<TGroupMember, TNamespaceRoleAssignment>(
            organizationId, principalId, namespaceRoles);

        var namespacesFromGroupStackRoles = StackRolesFromGroupQuery<TGroupMember, TStackRoleAssignment>(
            organizationId, principalId, stackRoles);

        var namespacesFromGroupOrganizationRoles = OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
            organizationId, principalId, organizationRoles);

        return namespacesFromNamespaceRoles
            .Concat(namespacesFromStackRoles)
            .Concat(namespacesFromOrganizationRoles)
            .Concat(namespacesFromGroupNamespaceRoles)
            .Concat(namespacesFromGroupStackRoles)
            .Concat(namespacesFromGroupOrganizationRoles);
    }

    private IQueryable<Namespace> NamespaceRolesFromGroupQuery<TGroupMember, TNamespaceRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<NamespaceRole> namespaceRoles)
        where TGroupMember : class, IGroupMember
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
    {
        return from ns in Repository.DbContext.Namespaces
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on ns.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupNamespaceRoleAssignments
                on new { NamespaceId = ns.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.NamespaceId, assignment.OrganizationId, assignment.PrincipalId }
            where ns.OrganizationId == organizationId
                  && namespaceRoles.Contains(assignment.RoleName)
            select ns;
    }

    private IQueryable<Namespace> StackRolesFromGroupQuery<TGroupMember, TStackRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<StackRole> stackRoles)
        where TGroupMember : class, IGroupMember
        where TStackRoleAssignment : class, IStackRoleAssignment
    {
        return from ns in Repository.DbContext.Namespaces
            join stack in Repository.DbContext.Stacks
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on ns.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupStackRoleAssignments
                on new { StackId = stack.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.StackId, assignment.OrganizationId, assignment.PrincipalId }
            where ns.OrganizationId == organizationId
                  && stackRoles.Contains(assignment.RoleName)
            select ns;
    }

    private IQueryable<Namespace> OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles)
        where TGroupMember : class, IGroupMember
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
    {
        return from ns in Repository.DbContext.Namespaces
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on ns.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.OrganizationId, assignment.PrincipalId }
            where ns.OrganizationId == organizationId
                  && organizationRoles.Contains(assignment.RoleName)
            select ns;
    }

    // Any role on a contained Module suffices, so these queries do not filter on
    // role names (see ReverseInheritedReadMetadataPermissionMap).
    private IQueryable<Namespace>? ReverseInheritanceQuery(Guid organizationId)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => ReverseInheritanceDirectQuery<UserModuleRoleAssignment>(organizationId, principalId)
                .Concat(ReverseInheritanceGroupQuery<UserGroupMember>(organizationId, principalId)),
            PrincipalDiscriminator.ServicePrincipal => ReverseInheritanceDirectQuery<ServicePrincipalModuleRoleAssignment>(organizationId, principalId)
                .Concat(ReverseInheritanceGroupQuery<ServicePrincipalGroupMember>(organizationId, principalId)),
            _ => null
        };
    }

    private IQueryable<Namespace> ReverseInheritanceDirectQuery<TModuleRoleAssignment>(
        Guid organizationId,
        Guid principalId)
        where TModuleRoleAssignment : class, IModuleRoleAssignment
    {
        var roles = ReverseInheritedReadMetadataPermissionMap.ModuleRoles;

        return from assignment in Repository.DbContext.Set<TModuleRoleAssignment>()
            where assignment.PrincipalId == principalId
                  && assignment.OrganizationId == organizationId
                  && roles.Contains(assignment.RoleName)
            join module in Repository.DbContext.Modules
                on new { assignment.ModuleId, assignment.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            select ns;
    }

    private IQueryable<Namespace> ReverseInheritanceGroupQuery<TGroupMember>(
        Guid organizationId,
        Guid principalId)
        where TGroupMember : class, IGroupMember
    {
        var moduleRoles = ReverseInheritedReadMetadataPermissionMap.ModuleRoles;

        return from groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupModuleRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.OrganizationId, assignment.PrincipalId }
            where moduleRoles.Contains(assignment.RoleName)
            join module in Repository.DbContext.Modules
                on new { assignment.ModuleId, assignment.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            select ns;
    }

    private bool CanCreateInStack<TOrganizationRoleAssignment, TStackRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        Guid stackId)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStackRoleAssignment : class, IStackRoleAssignment
    {
        // Check direct organization role assignment
        var hasOrgPermission = Repository.DbContext.Set<TOrganizationRoleAssignment>()
            .Any(ra => ra.OrganizationId == organizationId
                       && ra.PrincipalId == principalId
                       && CreatePermissionMap.OrganizationRoles.Contains(ra.RoleName));

        if (hasOrgPermission)
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

        if (hasOrgPermissionViaGroup)
            return true;

        // Check direct stack role assignment
        var hasStackPermission = Repository.DbContext.Set<TStackRoleAssignment>()
            .Any(ra => ra.StackId == stackId
                       && ra.OrganizationId == organizationId
                       && ra.PrincipalId == principalId
                       && CreatePermissionMap.StackRoles.Contains(ra.RoleName));

        if (hasStackPermission)
            return true;

        // Check group-based stack role assignment
        var hasStackPermissionViaGroup = PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => (
                from gum in Repository.DbContext.UserGroupMembers
                where gum.UserId == principalId && gum.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupStackRoleAssignments
                    on new { StackId = stackId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.StackId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where CreatePermissionMap.StackRoles.Contains(assignment.RoleName)
                select assignment
            ).Any(),
            PrincipalDiscriminator.ServicePrincipal => (
                from gspm in Repository.DbContext.ServicePrincipalGroupMembers
                where gspm.ServicePrincipalId == principalId && gspm.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gspm.GroupId, RootOrganizationId = gspm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupStackRoleAssignments
                    on new { StackId = stackId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.StackId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where CreatePermissionMap.StackRoles.Contains(assignment.RoleName)
                select assignment
            ).Any(),
            _ => false
        };

        return hasStackPermissionViaGroup;
    }

    #endregion

    #region public methods

    public async Task<Namespace> Get(Guid stackId, string name, Guid organizationId)
    {
        var @namespace = await Repository.Get(stackId, name, organizationId);

        if (!CanRead(@namespace.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(Namespace)} with ID {@namespace.Id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return @namespace;
    }

    public async Task<Namespace> Get(string stackName, string name, Guid organizationId)
    {
        var @namespace = await Repository.Get(stackName, name, organizationId);

        if (!CanRead(@namespace.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(Namespace)} with ID {@namespace.Id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return @namespace;
    }

    public Task<NamespaceMetadata> GetMetadata(Guid stackId, string name, Guid organizationId)
        => GetMetadataByKey(
            organizationId,
            async () => (await Repository.Get(stackId, name, organizationId)).Id,
            id => Repository.GetMetadata(id, organizationId),
            $"stack {stackId} and name \"{name}\"");

    public Task<NamespaceMetadata> GetMetadata(string stackName, string name, Guid organizationId)
        => GetMetadataByKey(
            organizationId,
            async () => (await Repository.Get(stackName, name, organizationId)).Id,
            id => Repository.GetMetadata(id, organizationId),
            $"stack \"{stackName}\" and name \"{name}\"");

    #endregion
}