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
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

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
    AgentReadDto,
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
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader, OrganizationRole.AgentContributor, OrganizationRole.AgentReader]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.AgentContributor]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.AgentContributor, OrganizationRole.AgentCreator]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.AgentContributor]
    };

    public async Task<Agent> GetByName(string name, Guid organizationId)
    {
        var entity = await Repository.GetByName(name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(Agent)} with ID {entity.Id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return entity;
    }

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
