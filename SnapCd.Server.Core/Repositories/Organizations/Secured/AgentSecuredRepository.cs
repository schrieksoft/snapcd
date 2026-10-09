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
using SnapCd.Contracts.Dto.Agents;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Views;
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

public class AgentSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<AgentRepositorySettings> options)
{
    public AgentSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new AgentSecuredRepository(
            new AgentRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class AgentSecuredRepository : GenericOrganizationChildSecuredRepository<
    Agent,
    AgentReadDto, AgentMetadata,
    AgentRepository,
    AgentCreatedEvent,
    AgentUpdatedEvent,
    AgentDeletedEvent,
    AgentRepositorySettings>
{
    public AgentSecuredRepository(
        AgentRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader, OrganizationRole.AgentContributor, OrganizationRole.AgentReader],
        AgentRoles = [AgentRole.Owner, AgentRole.Contributor, AgentRole.Reader]
    };
    /// <summary>
    /// Wider than read by MetadataReader, which a supply to a readable scope derives.
    /// </summary>
    public override PermissionMap ReadMetadataPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader, OrganizationRole.AgentContributor, OrganizationRole.AgentReader],
        AgentRoles = [AgentRole.Owner, AgentRole.Contributor, AgentRole.Reader, AgentRole.MetadataReader]
    };




    public async Task<AgentMetadata> GetMetadata(Guid id, Guid organizationId)
    {
        if (!CanReadMetadata(id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(Agent)} with ID {id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read its metadata.");

        return await ReadMetadataQuery(organizationId).FirstAsync(a => a.Id == id && a.OrganizationId == organizationId);
    }

    public async Task<int> CountMetadata(
        Guid organizationId,
        Func<IQueryable<AgentMetadata>, IQueryable<AgentMetadata>>? queryModifier = null)
    {
        var query = ReadMetadataQuery(organizationId).Distinct();

        if (queryModifier != null)
            query = queryModifier(query);

        return await query.CountAsync();
    }
    public async Task<List<AgentMetadata>> ListMetadata(
        Guid organizationId,
        Func<IQueryable<AgentMetadata>, IQueryable<AgentMetadata>>? queryModifier = null,
        Func<IQueryable<AgentMetadata>, IOrderedQueryable<AgentMetadata>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var query = ReadMetadataQuery(organizationId).Distinct();

        if (queryModifier != null)
            query = queryModifier(query);

        if (orderBy != null)
            query = orderBy(query);

        if (pageNumber.HasValue && pageSize.HasValue)
            query = query.Skip((pageNumber.Value - 1) * pageSize.Value);

        if (pageSize.HasValue)
            query = query.Take(pageSize.Value);

        return await query.ToListAsync();
    }
    public override bool CanReadMetadata(Guid id, Guid organizationId)
    {
        return ReadMetadataQuery(organizationId).Any(a => a.Id == id && a.OrganizationId == organizationId);
    }
    public IQueryable<AgentMetadata> ReadMetadataQuery(Guid organizationId)
        => ReadMetadataOrganizationRoleQuery(organizationId)
            .Concat(AgentRoleQuery(organizationId, ReadMetadataPermissionMap.AgentRoles))
            .Concat(SuppliedScopeMetadataQuery(organizationId))
            .Select(x => new AgentMetadata { Id = x.Id, OrganizationId = x.OrganizationId, Name = x.Name });

    /// <summary>Supplying a Agent to a scope is what makes it usable there, so a role on that
    /// scope carries metadata read on the Agent itself. Derived on write by trigger.</summary>
    private IQueryable<Agent> SuppliedScopeMetadataQuery(Guid organizationId)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return from entity in Repository.DbContext.Agents
            join derived in Repository.DbContext.DerivedAgentRoleAssignments
                on new { EntityId = entity.Id, entity.OrganizationId }
                equals new { EntityId = derived.AgentId, derived.OrganizationId }
            where entity.OrganizationId == organizationId
                  && derived.PrincipalId == principalId
                  && derived.PrincipalDiscriminator == PrincipalDiscriminator
                  && ReadMetadataPermissionMap.AgentRoles.Contains(derived.RoleName)
            select entity;
    }


    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.AgentContributor],
        AgentRoles = [AgentRole.Owner, AgentRole.Contributor]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.AgentContributor, OrganizationRole.AgentCreator]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.AgentContributor],
        AgentRoles = [AgentRole.Owner, AgentRole.Contributor]
    };

    public override IQueryable<Agent> ReadQuery(Guid organizationId)
        => base.ReadQuery(organizationId).Concat(AgentRoleQuery(organizationId, ReadPermissionMap.AgentRoles));

    public override IQueryable<Agent> UpdateQuery(Guid organizationId)
        => base.UpdateQuery(organizationId).Concat(AgentRoleQuery(organizationId, UpdatePermissionMap.AgentRoles));

    public override IQueryable<Agent> DeleteQuery(Guid organizationId)
        => base.DeleteQuery(organizationId).Concat(AgentRoleQuery(organizationId, DeletePermissionMap.AgentRoles));

    private IQueryable<Agent> AgentRoleQuery(Guid organizationId, List<AgentRole> roles)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => AgentRoleQuery<UserAgentRoleAssignment, UserGroupMember>(organizationId, principalId, roles),
            PrincipalDiscriminator.ServicePrincipal => AgentRoleQuery<ServicePrincipalAgentRoleAssignment, ServicePrincipalGroupMember>(organizationId, principalId, roles),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    private IQueryable<Agent> AgentRoleQuery<TRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<AgentRole> roles)
        where TRoleAssignment : class, IAgentRoleAssignment
        where TGroupMember : class, IGroupMember
    {
        var direct =
            from entity in Repository.DbContext.Agents
            join assignment in Repository.DbContext.Set<TRoleAssignment>()
                on new { AgentId = entity.Id, entity.OrganizationId } equals new { assignment.AgentId, assignment.OrganizationId }
            where entity.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && roles.Contains(assignment.RoleName)
            select entity;

        var group =
            from entity in Repository.DbContext.Agents
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupAgentRoleAssignments
                on new { AgentId = entity.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.AgentId, assignment.OrganizationId, assignment.PrincipalId }
            where entity.OrganizationId == organizationId
                  && roles.Contains(assignment.RoleName)
            select entity;

        return direct.Concat(group);
    }


    public async Task<Agent> GetByName(string name, Guid organizationId)
    {
        var entity = await Repository.GetByName(name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(Agent)} with ID {entity.Id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return entity;
    }

    public Task<AgentMetadata> GetMetadataByName(string name, Guid organizationId)
        => GetMetadataByKey(
            organizationId,
            async () => (await Repository.GetByName(name, organizationId)).Id,
            id => ReadMetadataQuery(organizationId).FirstAsync(x => x.Id == id),
            $"name \"{name}\"");

    /// <summary>
    /// Used by the token-issuance code path (Phase 5) — bypasses CanRead since the caller
    /// is the token endpoint itself validating that an SP is bound to an Agent, not the
    /// SP looking up another principal's data.
    /// </summary>
    public async Task<Agent?> GetByServicePrincipalId(Guid servicePrincipalId, Guid organizationId)
    {
        return await Repository.GetByServicePrincipalId(servicePrincipalId, organizationId);
    }
}
