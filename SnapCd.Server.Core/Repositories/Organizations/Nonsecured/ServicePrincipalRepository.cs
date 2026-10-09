// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Linq.Expressions;
using MassTransit;
using OpenIddict.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ServicePrincipals;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ServicePrincipalRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ServicePrincipalRepositorySettings> options)
{
    public ServicePrincipalRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ServicePrincipalRepository(dbContext, principalProvider, bus, options);
    }
}

public class ServicePrincipalRepository : GenericOrganizationChildRepository<ServicePrincipal, ServicePrincipalReadDto, ServicePrincipalMetadata, ServicePrincipalCreatedEvent, ServicePrincipalUpdatedEvent,
    ServicePrincipalDeletedEvent, ServicePrincipalRepositorySettings>
{

    protected override Expression<Func<ServicePrincipal, ServicePrincipalMetadata>> MetadataProjection =>
        e => new ServicePrincipalMetadata
        {
            Id = e.Id,
            OrganizationId = e.OrganizationId,
            // Stored with the organization as a prefix; callers get the name on its own.
            ClientId = e.ClientId!.Substring(e.OrganizationId.ToString().Length + 1),
            DisplayName = e.DisplayName
        };
    public ServicePrincipalRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ServicePrincipalRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ServicePrincipalReadDto MapToDto(ServicePrincipal entity)
    {
        return ServicePrincipalMapper.ToDto(entity);
    }

    // The entity backs every OpenIddict application, so the table also holds clients that are not
    // service principals. One is created with an organization-prefixed client id, a secret and the
    // client credentials grant, and the reads below return only those.
    private static readonly Expression<Func<ServicePrincipal, bool>> IsServicePrincipal =
        sp => sp.ClientType == OpenIddictConstants.ClientTypes.Confidential
              && sp.ClientId != null
              && sp.ClientId.StartsWith(sp.OrganizationId.ToString() + ":")
              && sp.Permissions != null
              && sp.Permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);

    // Narrows whatever the caller supplied, rather than standing in for it, so the restriction
    // holds however the read was reached.
    private IQueryable<ServicePrincipal> NarrowQuery(IQueryable<ServicePrincipal>? query)
        => (query ?? DbContext.ServicePrincipals).Where(IsServicePrincipal);

    private static Func<IQueryable<ServicePrincipal>, IQueryable<ServicePrincipal>> NarrowModifier(
        Func<IQueryable<ServicePrincipal>, IQueryable<ServicePrincipal>>? queryModifier)
        => q => queryModifier == null
            ? q.Where(IsServicePrincipal)
            : queryModifier(q.Where(IsServicePrincipal));

    public override Task<ServicePrincipal> Get(
        Guid id,
        Guid organizationId,
        Func<IQueryable<ServicePrincipal>, IQueryable<ServicePrincipal>>? queryModifier = null)
        => base.Get(id, organizationId, NarrowModifier(queryModifier));

    public override Task<TProjection> Get<TProjection>(
        Guid id,
        Guid organizationId,
        Func<IQueryable<ServicePrincipal>, IQueryable<TProjection>> projection)
        => base.Get(id, organizationId, q => projection(NarrowModifier(null)(q)));

    public override Task<int> Count(
        Guid organizationId,
        IQueryable<ServicePrincipal>? query = null,
        Func<IQueryable<ServicePrincipal>, IQueryable<ServicePrincipal>>? queryModifier = null)
        => base.Count(organizationId, NarrowQuery(query), queryModifier);

    public override Task<List<ServicePrincipal>> List(
        Guid organizationId,
        IQueryable<ServicePrincipal>? query = null,
        Func<IQueryable<ServicePrincipal>, IQueryable<ServicePrincipal>>? queryModifier = null,
        Func<IQueryable<ServicePrincipal>, IOrderedQueryable<ServicePrincipal>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
        => base.List(organizationId, NarrowQuery(query), queryModifier, orderBy, pageNumber, pageSize);

    public override Task<List<TProjection>> List<TProjection>(
        Guid organizationId,
        Func<IQueryable<ServicePrincipal>, IQueryable<TProjection>> projection,
        IQueryable<ServicePrincipal>? query = null,
        Func<IQueryable<TProjection>, IOrderedQueryable<TProjection>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
        => base.List(organizationId, projection, NarrowQuery(query), orderBy, pageNumber, pageSize);

    public override Task<List<ServicePrincipal>> ListByParentId(
        Guid parentId,
        Guid organizationId,
        Func<IQueryable<ServicePrincipal>, IQueryable<ServicePrincipal>>? queryModifier = null,
        IQueryable<ServicePrincipal>? query = null,
        Func<IQueryable<ServicePrincipal>, IOrderedQueryable<ServicePrincipal>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
        => base.ListByParentId(parentId, organizationId, queryModifier, NarrowQuery(query), orderBy, pageNumber, pageSize);

    public override Task<List<TProjection>> ListByParentId<TProjection>(
        Guid parentId,
        Guid organizationId,
        Func<IQueryable<ServicePrincipal>, IQueryable<TProjection>> projection,
        IQueryable<ServicePrincipal>? query = null,
        Func<IQueryable<TProjection>, IOrderedQueryable<TProjection>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
        => base.ListByParentId(parentId, organizationId, projection, NarrowQuery(query), orderBy, pageNumber, pageSize);

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ServicePrincipal entity)
    {
        var currentCount = await NarrowQuery(null)
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ServicePrincipalQuota), currentCount);
    }

    public async Task<ServicePrincipal?> GetByClientId(string clientId, Guid organizationId)
    {
        var prefixedClientId = $"{organizationId}:{clientId}";
        return await NarrowQuery(null)
            .Where(sp => sp.OrganizationId == organizationId)
            .SingleOrDefaultAsync(sp => sp.ClientId == prefixedClientId);
    }

    /// <summary>
    /// Checks if a ServicePrincipal can run a specific module via its assigned Runner.
    /// Checks runner assignments at module, namespace, and stack levels, as well as IsSuppliedToAllModules flag.
    /// </summary>
    public async Task<bool> CanRunModule(Guid servicePrincipalId, Guid moduleId, Guid organizationId)
    {
        // Get the module with namespace and stack information
        var moduleInfo = await DbContext.Modules
            .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
            .Select(m => new
            {
                m.Id,
                m.NamespaceId,
                StackId = m.Namespace.StackId
            })
            .FirstOrDefaultAsync();

        if (moduleInfo == null)
            return false;

        // Check if ServicePrincipal has a Runner assigned and that Runner has access to the module
        var hasAccess = await DbContext.Runners
            .Where(r => r.ServicePrincipalId == servicePrincipalId && r.OrganizationId == organizationId)
            .AnyAsync(r =>
                // Direct module assignment
                r.RunnerModuleSupplies.Any(a => a.ModuleId == moduleId) ||
                // Namespace-level assignment
                r.RunnerNamespaceSupplies.Any(a => a.NamespaceId == moduleInfo.NamespaceId) ||
                // Stack-level assignment
                r.RunnerStackSupplies.Any(a => a.StackId == moduleInfo.StackId) ||
                // Assigned to all modules
                r.IsSuppliedToAllModules);

        return hasAccess;
    }
}