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
using SnapCd.Contracts.Dto.Outputs;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.Outputs;

public class OutputSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<OutputRepositorySettings> options)
{
    public OutputSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new OutputSecuredRepository(
            new OutputRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class OutputSecuredRepository : GenericSecuredRepository<
    Output,
    OutputReadDto,
    OutputRepository,
    OutputCreatedEvent,
    OutputUpdatedEvent,
    OutputDeletedEvent,
    OutputRepositorySettings>
{
    public OutputSecuredRepository(
        OutputRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader, OrganizationRole.StackContributor, OrganizationRole.StackReader],
        StackRoles = [StackRole.Owner, StackRole.Contributor, StackRole.Reader],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor, NamespaceRole.Reader],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.Reader],
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [],
        StackRoles = [],
        NamespaceRoles = [],
        ModuleRoles = [],
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [],
        StackRoles = [],
        NamespaceRoles = [],
        ModuleRoles = [],
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor],
        ModuleRoles = [ModuleRole.Owner],
    };

    public override IQueryable<Output> ReadQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            ReadPermissionMap.OrganizationRoles,
            ReadPermissionMap.StackRoles,
            ReadPermissionMap.NamespaceRoles,
            ReadPermissionMap.ModuleRoles);
    }

    public override IQueryable<Output> CreateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            CreatePermissionMap.OrganizationRoles,
            CreatePermissionMap.StackRoles,
            CreatePermissionMap.NamespaceRoles,
            CreatePermissionMap.ModuleRoles);
    }

    public override IQueryable<Output> UpdateQuery(Guid organizationId)
    {
        // Outputs can never be updated
        return Enumerable.Empty<Output>().AsQueryable();
    }

    public override IQueryable<Output> DeleteQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            DeletePermissionMap.OrganizationRoles,
            DeletePermissionMap.StackRoles,
            DeletePermissionMap.NamespaceRoles,
            DeletePermissionMap.ModuleRoles);
    }

    public override bool CanRead(Guid id, Guid organizationId)
    {
        return ReadQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);
    }

    public override bool CanCreate(Guid parentId, Guid organizationId)
    {
        // parentId is OutputSetId - find the module ID for this OutputSet
        var moduleId = Repository.DbContext.OutputSets
            .Where(s => s.Id == parentId && s.OrganizationId == organizationId)
            .Select(s => s.ModuleId)
            .FirstOrDefault();

        if (moduleId == Guid.Empty)
            return false;

        // Check if user can create entities for this module
        return CreateQuery(organizationId)
            .Join(Repository.DbContext.OutputSets,
                output => output.OutputSetId,
                outputSet => outputSet.Id,
                (output, outputSet) => outputSet)
            .Any(outputSet => outputSet.ModuleId == moduleId && outputSet.OrganizationId == organizationId);
    }

    public override bool CanUpdate(Guid id, Guid organizationId)
    {
        // Outputs can never be updated
        return false;
    }

    public override bool CanDelete(Guid id, Guid organizationId)
    {
        return DeleteQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);
    }

    public override string GetParentEntityName()
    {
        return "OutputSet";
    }

    private IQueryable<Output> RoleQueryDispatch(
        Guid organizationId,
        List<OrganizationRole> organizationRoles,
        List<StackRole> stackRoles,
        List<NamespaceRole> namespaceRoles,
        List<ModuleRole> moduleRoles)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => RoleQuery<
                UserOrganizationRoleAssignment,
                UserStackRoleAssignment,
                UserNamespaceRoleAssignment,
                UserModuleRoleAssignment,
                UserGroupMember>(
                organizationId, principalId, organizationRoles, stackRoles, namespaceRoles, moduleRoles),
            PrincipalDiscriminator.ServicePrincipal => RoleQuery<
                ServicePrincipalOrganizationRoleAssignment,
                ServicePrincipalStackRoleAssignment,
                ServicePrincipalNamespaceRoleAssignment,
                ServicePrincipalModuleRoleAssignment,
                ServicePrincipalGroupMember>(
                organizationId, principalId, organizationRoles, stackRoles, namespaceRoles, moduleRoles),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    private IQueryable<Output> RoleQuery<TOrganizationRoleAssignment, TStackRoleAssignment, TNamespaceRoleAssignment, TModuleRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles,
        List<StackRole> stackRoles,
        List<NamespaceRole> namespaceRoles,
        List<ModuleRole> moduleRoles)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStackRoleAssignment : class, IStackRoleAssignment
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
        where TModuleRoleAssignment : class, IModuleRoleAssignment
        where TGroupMember : class, IGroupMember
    {
        // Direct role assignments
        var entitiesFromModuleRoles =
            from output in Repository.DbContext.Outputs
            join outputSet in Repository.DbContext.OutputSets
                on output.OutputSetId equals outputSet.Id
            join assignment in Repository.DbContext.Set<TModuleRoleAssignment>()
                on new { ModuleId = outputSet.ModuleId, outputSet.OrganizationId } equals new { assignment.ModuleId, assignment.OrganizationId }
            where output.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && moduleRoles.Contains(assignment.RoleName)
            select output;

        var entitiesFromNamespaceRoles =
            from output in Repository.DbContext.Outputs
            join outputSet in Repository.DbContext.OutputSets
                on output.OutputSetId equals outputSet.Id
            join module in Repository.DbContext.Modules
                on new { ModuleId = outputSet.ModuleId, outputSet.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join assignment in Repository.DbContext.Set<TNamespaceRoleAssignment>()
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { assignment.NamespaceId, assignment.OrganizationId }
            where output.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && namespaceRoles.Contains(assignment.RoleName)
            select output;

        var entitiesFromStackRoles =
            from output in Repository.DbContext.Outputs
            join outputSet in Repository.DbContext.OutputSets
                on output.OutputSetId equals outputSet.Id
            join module in Repository.DbContext.Modules
                on new { ModuleId = outputSet.ModuleId, outputSet.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join stack in Repository.DbContext.Stacks
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join assignment in Repository.DbContext.Set<TStackRoleAssignment>()
                on new { StackId = stack.Id, stack.OrganizationId } equals new { assignment.StackId, assignment.OrganizationId }
            where output.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && stackRoles.Contains(assignment.RoleName)
            select output;

        var entitiesFromOrganizationRoles =
            from output in Repository.DbContext.Outputs
            join assignment in Repository.DbContext.Set<TOrganizationRoleAssignment>()
                on output.OrganizationId equals assignment.OrganizationId
            where output.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && organizationRoles.Contains(assignment.RoleName)
            select output;

        // Group-based role assignments
        var entitiesFromGroupModuleRoles = ModuleRolesFromGroupQuery<TGroupMember, TModuleRoleAssignment>(
            organizationId, principalId, moduleRoles);

        var entitiesFromGroupNamespaceRoles = NamespaceRolesFromGroupQuery<TGroupMember, TNamespaceRoleAssignment>(
            organizationId, principalId, namespaceRoles);

        var entitiesFromGroupStackRoles = StackRolesFromGroupQuery<TGroupMember, TStackRoleAssignment>(
            organizationId, principalId, stackRoles);

        var entitiesFromGroupOrganizationRoles = OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
            organizationId, principalId, organizationRoles);

        return entitiesFromModuleRoles
            .Concat(entitiesFromNamespaceRoles)
            .Concat(entitiesFromStackRoles)
            .Concat(entitiesFromOrganizationRoles)
            .Concat(entitiesFromGroupModuleRoles)
            .Concat(entitiesFromGroupNamespaceRoles)
            .Concat(entitiesFromGroupStackRoles)
            .Concat(entitiesFromGroupOrganizationRoles);
    }

    private IQueryable<Output> ModuleRolesFromGroupQuery<TGroupMember, TModuleRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<ModuleRole> moduleRoles)
        where TGroupMember : class, IGroupMember
        where TModuleRoleAssignment : class, IModuleRoleAssignment
    {
        return from output in Repository.DbContext.Outputs
            join outputSet in Repository.DbContext.OutputSets
                on output.OutputSetId equals outputSet.Id
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on output.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupModuleRoleAssignments
                on new { ModuleId = outputSet.ModuleId, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.ModuleId, assignment.OrganizationId, assignment.PrincipalId }
            where output.OrganizationId == organizationId
                  && moduleRoles.Contains(assignment.RoleName)
            select output;
    }

    private IQueryable<Output> NamespaceRolesFromGroupQuery<TGroupMember, TNamespaceRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<NamespaceRole> namespaceRoles)
        where TGroupMember : class, IGroupMember
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
    {
        return from output in Repository.DbContext.Outputs
            join outputSet in Repository.DbContext.OutputSets
                on output.OutputSetId equals outputSet.Id
            join module in Repository.DbContext.Modules
                on new { ModuleId = outputSet.ModuleId, outputSet.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on output.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupNamespaceRoleAssignments
                on new { NamespaceId = module.NamespaceId, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.NamespaceId, assignment.OrganizationId, assignment.PrincipalId }
            where output.OrganizationId == organizationId
                  && namespaceRoles.Contains(assignment.RoleName)
            select output;
    }

    private IQueryable<Output> StackRolesFromGroupQuery<TGroupMember, TStackRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<StackRole> stackRoles)
        where TGroupMember : class, IGroupMember
        where TStackRoleAssignment : class, IStackRoleAssignment
    {
        return from output in Repository.DbContext.Outputs
            join outputSet in Repository.DbContext.OutputSets
                on output.OutputSetId equals outputSet.Id
            join module in Repository.DbContext.Modules
                on new { ModuleId = outputSet.ModuleId, outputSet.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join stack in Repository.DbContext.Stacks
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on output.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupStackRoleAssignments
                on new { StackId = stack.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.StackId, assignment.OrganizationId, assignment.PrincipalId }
            where output.OrganizationId == organizationId
                  && stackRoles.Contains(assignment.RoleName)
            select output;
    }

    private IQueryable<Output> OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles)
        where TGroupMember : class, IGroupMember
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
    {
        return from output in Repository.DbContext.Outputs
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on output.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.OrganizationId, assignment.PrincipalId }
            where output.OrganizationId == organizationId
                  && organizationRoles.Contains(assignment.RoleName)
            select output;
    }


    public async Task<List<Output>> ListByOutputSetIds(List<Guid> outputSetIds, Guid organizationId)
    {
        var outputs = await Repository.ListByOutputSetIds(outputSetIds, organizationId);

        foreach (var output in outputs)
            if (!CanRead(output.Id, organizationId))
                throw new UnauthorizedAccessException($"Access denied to Output {output.Id}");

        return outputs;
    }

    public async Task<List<Output>> ListByIds(List<Guid> outputIds, Guid organizationId)
    {
        var outputs = await Repository.ListByIds(outputIds, organizationId);

        foreach (var output in outputs)
            if (!CanRead(output.Id, organizationId))
                throw new UnauthorizedAccessException($"Access denied to Output {output.Id}");

        return outputs;
    }
}