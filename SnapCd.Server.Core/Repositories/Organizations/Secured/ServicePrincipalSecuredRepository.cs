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
using SnapCd.Contracts.Dto.ServicePrincipals;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ServicePrincipalSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ServicePrincipalRepositorySettings> options)
{
    public ServicePrincipalSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ServicePrincipalSecuredRepository(
            new ServicePrincipalRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ServicePrincipalSecuredRepository : GenericOrganizationChildSecuredRepository<
    ServicePrincipal,
    ServicePrincipalReadDto,
    ServicePrincipalRepository,
    ServicePrincipalCreatedEvent,
    ServicePrincipalUpdatedEvent,
    ServicePrincipalDeletedEvent,
    ServicePrincipalRepositorySettings>
{
    public ServicePrincipalSecuredRepository(
        ServicePrincipalRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager]
    };

    public override PermissionMap ReadMetadataPermissionMap => new()
    {
        OrganizationRoles =
        [
            OrganizationRole.Owner, OrganizationRole.IdentityAccessManager,
            OrganizationRole.IdentityAccessMetadataReader
        ]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager]
    };

    public async Task<ServicePrincipal?> GetByClientId(string clientId, Guid organizationId)
    {
        var entity = await Repository.GetByClientId(clientId, organizationId);
        
        if (entity == null)
            throw new EntityNotFoundException($"Unable to find ServicePrincipal with ClientId \"{clientId}\"");
        
        if (!CanRead(entity.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(ServicePrincipal)} with ID {entity.Id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return entity;
        
    }

    public IQueryable<ServicePrincipalMetadata> ReadMetadataQuery(Guid organizationId)
    {
        return ReadMetadataOrganizationRoleQuery(organizationId)
            .Select(x => new ServicePrincipalMetadata
            {
                Id = x.Id,
                OrganizationId = x.OrganizationId,
                // Stored with the organization as a prefix; callers get the name on its own.
                ClientId = x.ClientId!.Substring(x.OrganizationId.ToString().Length + 1),
                DisplayName = x.DisplayName
            });
    }

    public override bool CanReadMetadata(Guid id, Guid organizationId)
    {
        return ReadMetadataQuery(organizationId).Any(x => x.Id == id && x.OrganizationId == organizationId);
    }

    public async Task<ServicePrincipalMetadata> GetMetadata(Guid id, Guid organizationId)
    {
        if (!CanReadMetadata(id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(ServicePrincipal)} with ID {id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read its metadata.");

        return await ReadMetadataQuery(organizationId).FirstAsync(x => x.Id == id && x.OrganizationId == organizationId);
    }

    /// <summary>The client id here is the bare name, as the metadata view exposes it, not the stored prefixed form.</summary>
    public async Task<ServicePrincipalMetadata> GetMetadataByClientId(string clientId, Guid organizationId)
    {
        var view = await ReadMetadataQuery(organizationId)
            .FirstOrDefaultAsync(x => x.ClientId == clientId && x.OrganizationId == organizationId);

        if (view == null)
            throw new EntityNotFoundException($"Unable to find {nameof(ServicePrincipal)} with client ID \"{clientId}\"");

        return view;
    }

    public async Task<int> CountMetadata(
        Guid organizationId,
        Func<IQueryable<ServicePrincipalMetadata>, IQueryable<ServicePrincipalMetadata>>? queryModifier = null)
    {
        var query = ReadMetadataQuery(organizationId).Distinct();

        if (queryModifier != null)
            query = queryModifier(query);

        return await query.CountAsync();
    }

    public async Task<List<ServicePrincipalMetadata>> ListMetadata(
        Guid organizationId,
        Func<IQueryable<ServicePrincipalMetadata>, IQueryable<ServicePrincipalMetadata>>? queryModifier = null,
        Func<IQueryable<ServicePrincipalMetadata>, IOrderedQueryable<ServicePrincipalMetadata>>? orderBy = null,
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
}
