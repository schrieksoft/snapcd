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
using SnapCd.Contracts.Dto.Runners;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class RunnerSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<RunnerRepositorySettings> options)
{
    public RunnerSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new RunnerSecuredRepository(
            new RunnerRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class RunnerSecuredRepository : GenericOrganizationChildSecuredRepository<
    Runner,
    RunnerReadDto,
    RunnerRepository,
    RunnerCreatedEvent,
    RunnerUpdatedEvent,
    RunnerDeletedEvent,
    RunnerRepositorySettings>
{
    public RunnerSecuredRepository(
        RunnerRepository repository,
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
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.RunnerContributor],
        RunnerRoles = [RunnerRole.Owner, RunnerRole.Contributor]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.RunnerContributor, OrganizationRole.RunnerCreator]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.RunnerContributor],
        RunnerRoles = [RunnerRole.Owner, RunnerRole.Contributor]
    };

    public override IQueryable<Runner> ReadQuery(Guid organizationId)
        => base.ReadQuery(organizationId).Concat(RunnerRoleQuery(organizationId, ReadPermissionMap.RunnerRoles));

    public override IQueryable<Runner> UpdateQuery(Guid organizationId)
        => base.UpdateQuery(organizationId).Concat(RunnerRoleQuery(organizationId, UpdatePermissionMap.RunnerRoles));

    public override IQueryable<Runner> DeleteQuery(Guid organizationId)
        => base.DeleteQuery(organizationId).Concat(RunnerRoleQuery(organizationId, DeletePermissionMap.RunnerRoles));

    private IQueryable<Runner> RunnerRoleQuery(Guid organizationId, List<RunnerRole> roles)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => RunnerRoleQuery<UserRunnerRoleAssignment, UserGroupMember>(organizationId, principalId, roles),
            PrincipalDiscriminator.ServicePrincipal => RunnerRoleQuery<ServicePrincipalRunnerRoleAssignment, ServicePrincipalGroupMember>(organizationId, principalId, roles),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    private IQueryable<Runner> RunnerRoleQuery<TRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<RunnerRole> roles)
        where TRoleAssignment : class, IRunnerRoleAssignment
        where TGroupMember : class, IGroupMember
    {
        var direct =
            from entity in Repository.DbContext.Runners
            join assignment in Repository.DbContext.Set<TRoleAssignment>()
                on new { RunnerId = entity.Id, entity.OrganizationId } equals new { assignment.RunnerId, assignment.OrganizationId }
            where entity.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && roles.Contains(assignment.RoleName)
            select entity;

        var group =
            from entity in Repository.DbContext.Runners
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupRunnerRoleAssignments
                on new { RunnerId = entity.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.RunnerId, assignment.OrganizationId, assignment.PrincipalId }
            where entity.OrganizationId == organizationId
                  && roles.Contains(assignment.RoleName)
            select entity;

        return direct.Concat(group);
    }


    public async Task<Runner> GetByName(string name, Guid organizationId)
    {
        var entity = await Repository.GetByName(name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(Runner)} with ID {entity.Id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return entity;
    }

    public async Task<List<Runner>> ListAssignedToModule(Guid moduleId, Guid organizationId)
    {
        return await Repository.ListAssignedToModule(moduleId, organizationId, ReadQuery(organizationId));
    }

    public async Task<List<Runner>> ListAssignedToNamespace(Guid namespaceId, Guid organizationId)
    {
        return await Repository.ListAssignedToNamespace(namespaceId, organizationId, ReadQuery(organizationId));
    }

    public async Task<List<Runner>> ListAssignedToStack(Guid stackId, Guid organizationId)
    {
        return await Repository.ListAssignedToStack(stackId, organizationId, ReadQuery(organizationId));
    }

    /// <summary>
    /// Checks if the current principal is the ServicePrincipal assigned to the specified Runner by name.
    /// Only ServicePrincipals can act as runners.
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="runnerName">The name of the Runner</param>
    /// <returns>True if the principal can act as a runner for this pool, false otherwise</returns>
    public bool CanActAsRunner(Guid organizationId, string runnerName)
    {
        // Only ServicePrincipals can act as runners
        if (PrincipalDiscriminator != PrincipalDiscriminator.ServicePrincipal)
            return false;

        var principalId = PrincipalProvider.GetSubject(organizationId);

        // Check if any runner with this name has this ServicePrincipal assigned
        return Repository.DbContext.Runners
            .Any(r => r.Name == runnerName
                && r.OrganizationId == organizationId
                && r.ServicePrincipalId == principalId);
    }

    /// <summary>
    /// Returns a list of all Runners in the organization with a flag indicating whether the current principal can act as a runner for each pool.
    /// Only ServicePrincipals can act as runners.
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <returns>List of RunnerCheckView objects</returns>
    public async Task<List<RunnerCheckView>> ListCanActAsRunner(Guid organizationId)
    {
        // Only ServicePrincipals can act as runners
        if (PrincipalDiscriminator != PrincipalDiscriminator.ServicePrincipal)
        {
            // Return all runners with CanActAsRunner = false for non-ServicePrincipals
            return await Repository.DbContext.Runners
                .Where(r => r.OrganizationId == organizationId)
                .Select(r => new RunnerCheckView
                {
                    Id = r.Id,
                    Name = r.Name,
                    CanActAsRunner = false
                })
                .ToListAsync();
        }

        var principalId = PrincipalProvider.GetSubject(organizationId);

        // Get all runners in the organization with flag indicating if this ServicePrincipal is assigned
        return await Repository.DbContext.Runners
            .Where(r => r.OrganizationId == organizationId)
            .Select(r => new RunnerCheckView
            {
                Id = r.Id,
                Name = r.Name,
                CanActAsRunner = r.ServicePrincipalId == principalId
            })
            .ToListAsync();
    }

}