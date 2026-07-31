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
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos.ModuleJobs;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ModuleJobSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModuleJobRepositorySettings> options)
{
    public ModuleJobSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleJobSecuredRepository(
            new ModuleJobRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ModuleJobSecuredRepository : GenericModuleChildSecuredRepository<
    ModuleJob,
    ModuleJobReadDto,
    ModuleJobRepository,
    ModuleJobCreatedEvent,
    ModuleJobUpdatedEvent,
    ModuleJobDeletedEvent,
    ModuleJobRepositorySettings>
{
    public ModuleJobSecuredRepository(
        ModuleJobRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<string?> GetActualDefinitiveRevision(Guid moduleId, Guid organizationId)
    {
        if (!CanReadForModuleId(moduleId, organizationId))
            return null;

        return await Repository.GetActualDefinitiveRevision(moduleId, organizationId);
    }

    public async Task<(string? DefinitiveRevision, string? DefinitiveClosureHash)> GetActualDefinitiveState(Guid moduleId, Guid organizationId)
    {
        if (!CanReadForModuleId(moduleId, organizationId))
            return (null, null);

        return await Repository.GetActualDefinitiveState(moduleId, organizationId);
    }

    public async Task<string?> GetLastAttemptedDefinitiveRevision(Guid moduleId, Guid organizationId)
    {
        if (!CanReadForModuleId(moduleId, organizationId))
            return null;
        
        return await Repository.GetLastAttemptedDefinitiveRevision(moduleId, organizationId);
    }

    public async Task<ActualStateHeadline?> GetCurrentActualStateHeadline(Guid moduleId, Guid organizationId)
    {
        if (!CanReadForModuleId(moduleId, organizationId))
            return null;
        
        return await Repository.GetCurrentActualStateHeadline(moduleId, organizationId);
    }

    
    public bool CanReadForModuleId(Guid moduleId, Guid organizationId)
    {
        return ReadQuery(organizationId).Any(e => e.ModuleId == moduleId && e.OrganizationId == organizationId);
    }
    

    public PermissionMap RunJobPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor, OrganizationRole.JobManager],
        StackRoles = [StackRole.Owner, StackRole.Contributor, StackRole.JobManager],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor, NamespaceRole.JobManager],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.Contributor, ModuleRole.JobManager]
    };

    public bool CanRunJob(Guid parentId, Guid organizationId)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => CanRunJobInModule<
                UserOrganizationRoleAssignment,
                UserStackRoleAssignment,
                UserNamespaceRoleAssignment,
                UserModuleRoleAssignment>(
                organizationId, principalId, parentId),
            PrincipalDiscriminator.ServicePrincipal => CanRunJobInModule<
                ServicePrincipalOrganizationRoleAssignment,
                ServicePrincipalStackRoleAssignment,
                ServicePrincipalNamespaceRoleAssignment,
                ServicePrincipalModuleRoleAssignment>(
                organizationId, principalId, parentId),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    protected bool CanRunJobInModule<TOrganizationRoleAssignment, TStackRoleAssignment, TNamespaceRoleAssignment, TModuleRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        Guid moduleId)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStackRoleAssignment : class, IStackRoleAssignment
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
        where TModuleRoleAssignment : class, IModuleRoleAssignment
    {
        // Check direct module role assignment
        var hasModulePermission = Repository.DbContext.Set<TModuleRoleAssignment>()
            .Any(ra => ra.ModuleId == moduleId
                       && ra.OrganizationId == organizationId
                       && ra.PrincipalId == principalId
                       && RunJobPermissionMap.ModuleRoles.Contains(ra.RoleName));

        if (hasModulePermission)
            return true;

        // Check direct namespace role assignment
        var hasNamespacePermission = (
            from module in Repository.DbContext.Modules
            where module.Id == moduleId && module.OrganizationId == organizationId
            join assignment in Repository.DbContext.Set<TNamespaceRoleAssignment>()
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { assignment.NamespaceId, assignment.OrganizationId }
            where assignment.PrincipalId == principalId
                  && RunJobPermissionMap.NamespaceRoles.Contains(assignment.RoleName)
            select assignment
        ).Any();

        if (hasNamespacePermission)
            return true;

        // Check direct stack role assignment
        var hasStackPermission = (
            from module in Repository.DbContext.Modules
            where module.Id == moduleId && module.OrganizationId == organizationId
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join assignment in Repository.DbContext.Set<TStackRoleAssignment>()
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { assignment.StackId, assignment.OrganizationId }
            where assignment.PrincipalId == principalId
                  && RunJobPermissionMap.StackRoles.Contains(assignment.RoleName)
            select assignment
        ).Any();

        if (hasStackPermission)
            return true;

        // Check direct organization role assignment
        var hasOrgPermission = Repository.DbContext.Set<TOrganizationRoleAssignment>()
            .Any(ra => ra.OrganizationId == organizationId
                       && ra.PrincipalId == principalId
                       && RunJobPermissionMap.OrganizationRoles.Contains(ra.RoleName));

        if (hasOrgPermission)
            return true;

        // Check group-based role assignments
        var hasPermissionViaGroup = PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => (
                // Module role via group
                from gum in Repository.DbContext.UserGroupMembers
                where gum.UserId == principalId && gum.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupModuleRoleAssignments
                    on new { ModuleId = moduleId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.ModuleId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where RunJobPermissionMap.ModuleRoles.Contains(assignment.RoleName)
                select assignment
            ).Any() || (
                // Namespace role via group
                from module in Repository.DbContext.Modules
                where module.Id == moduleId && module.OrganizationId == organizationId
                join gum in Repository.DbContext.UserGroupMembers
                    on module.OrganizationId equals gum.OrganizationId
                where gum.UserId == principalId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupNamespaceRoleAssignments
                    on new { NamespaceId = module.NamespaceId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.NamespaceId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where RunJobPermissionMap.NamespaceRoles.Contains(assignment.RoleName)
                select assignment
            ).Any() || (
                // Stack role via group
                from module in Repository.DbContext.Modules
                where module.Id == moduleId && module.OrganizationId == organizationId
                join ns in Repository.DbContext.Namespaces
                    on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
                join gum in Repository.DbContext.UserGroupMembers
                    on module.OrganizationId equals gum.OrganizationId
                where gum.UserId == principalId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupStackRoleAssignments
                    on new { StackId = ns.StackId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.StackId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where RunJobPermissionMap.StackRoles.Contains(assignment.RoleName)
                select assignment
            ).Any() || (
                // Organization role via group
                from gum in Repository.DbContext.UserGroupMembers
                where gum.UserId == principalId && gum.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                    on new { OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where RunJobPermissionMap.OrganizationRoles.Contains(assignment.RoleName)
                select assignment
            ).Any(),
            PrincipalDiscriminator.ServicePrincipal => (
                // Module role via group
                from gspm in Repository.DbContext.ServicePrincipalGroupMembers
                where gspm.ServicePrincipalId == principalId && gspm.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gspm.GroupId, RootOrganizationId = gspm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupModuleRoleAssignments
                    on new { ModuleId = moduleId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.ModuleId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where RunJobPermissionMap.ModuleRoles.Contains(assignment.RoleName)
                select assignment
            ).Any() || (
                // Namespace role via group
                from module in Repository.DbContext.Modules
                where module.Id == moduleId && module.OrganizationId == organizationId
                join gspm in Repository.DbContext.ServicePrincipalGroupMembers
                    on module.OrganizationId equals gspm.OrganizationId
                where gspm.ServicePrincipalId == principalId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gspm.GroupId, RootOrganizationId = gspm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupNamespaceRoleAssignments
                    on new { NamespaceId = module.NamespaceId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.NamespaceId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where RunJobPermissionMap.NamespaceRoles.Contains(assignment.RoleName)
                select assignment
            ).Any() || (
                // Stack role via group
                from module in Repository.DbContext.Modules
                where module.Id == moduleId && module.OrganizationId == organizationId
                join ns in Repository.DbContext.Namespaces
                    on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
                join gspm in Repository.DbContext.ServicePrincipalGroupMembers
                    on module.OrganizationId equals gspm.OrganizationId
                where gspm.ServicePrincipalId == principalId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gspm.GroupId, RootOrganizationId = gspm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupStackRoleAssignments
                    on new { StackId = ns.StackId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.StackId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where RunJobPermissionMap.StackRoles.Contains(assignment.RoleName)
                select assignment
            ).Any() || (
                // Organization role via group
                from gspm in Repository.DbContext.ServicePrincipalGroupMembers
                where gspm.ServicePrincipalId == principalId && gspm.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gspm.GroupId, RootOrganizationId = gspm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                    on new { OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where RunJobPermissionMap.OrganizationRoles.Contains(assignment.RoleName)
                select assignment
            ).Any(),
            _ => false
        };

        return hasPermissionViaGroup;
    }

    public IQueryable<ModuleJob> RunJobQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            RunJobPermissionMap.OrganizationRoles,
            RunJobPermissionMap.StackRoles,
            RunJobPermissionMap.NamespaceRoles,
            RunJobPermissionMap.ModuleRoles);
    }

    public async Task<List<RunJobPermission>> ListHasRunJobPermission(List<ModuleNamespaceIdTuple> toCheck, Guid organizationId)
    {
        if (!toCheck.Any())
            return new List<RunJobPermission>();

        // Get all module IDs we need to check
        var moduleIdsToCheck = toCheck.Select(t => t.ModuleId).Distinct().ToList();

        // Get all jobs for modules where the user has run permission, filtered to our list
        var permittedModuleIds = await RunJobQuery(organizationId)
            .Where(j => moduleIdsToCheck.Contains(j.ModuleId))
            .Select(j => j.ModuleId)
            .Distinct()
            .ToListAsync();

        // Build results based on bulk permission check
        return toCheck.Select(tuple => new RunJobPermission
        {
            ModuleId = tuple.ModuleId,
            NamespaceId = tuple.NamespaceId,
            HasPermission = permittedModuleIds.Contains(tuple.ModuleId)
        }).ToList();
    }
}