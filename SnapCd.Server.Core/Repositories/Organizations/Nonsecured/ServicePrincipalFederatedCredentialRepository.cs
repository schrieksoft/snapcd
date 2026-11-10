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
using SnapCd.Contracts.Dto.ServicePrincipalFederatedCredentials;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Settings.Repositories;
using System.Linq.Expressions;
using SnapCd.Server.Core.Services.Interfaces.PrincipalProvider;
using SnapCd.Server.Core.Services.Nonsecured.PrincipalProvider;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ServicePrincipalFederatedCredentialRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ServicePrincipalFederatedCredentialRepositorySettings> options)
{
    public ServicePrincipalFederatedCredentialRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ServicePrincipalFederatedCredentialRepository(dbContext, principalProvider, bus, options);
    }
}

public class ServicePrincipalFederatedCredentialRepository : GenericOrganizationChildRepository<
    ServicePrincipalFederatedCredential,
    ServicePrincipalFederatedCredentialReadDto,
    ServicePrincipalFederatedCredentialMetadata,
    ServicePrincipalFederatedCredentialCreatedEvent,
    ServicePrincipalFederatedCredentialUpdatedEvent,
    ServicePrincipalFederatedCredentialDeletedEvent,
    ServicePrincipalFederatedCredentialRepositorySettings>
{
    protected override Expression<Func<ServicePrincipalFederatedCredential, ServicePrincipalFederatedCredentialMetadata>> MetadataProjection =>
        e => new ServicePrincipalFederatedCredentialMetadata
        {
            Id = e.Id,
            OrganizationId = e.OrganizationId,
            Name = e.Name,
            ServicePrincipalId = e.ServicePrincipalId
        };

    public ServicePrincipalFederatedCredentialRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ServicePrincipalFederatedCredentialRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ServicePrincipalFederatedCredentialReadDto MapToDto(ServicePrincipalFederatedCredential entity)
    {
        return ServicePrincipalFederatedCredentialMapper.ToDto(entity);
    }

    public async Task<ServicePrincipalFederatedCredential> GetByName(string name, Guid servicePrincipalId, Guid organizationId)
    {
        var credential = await DbContext.ServicePrincipalFederatedCredentials
            .Where(e => e.OrganizationId == organizationId && e.ServicePrincipalId == servicePrincipalId)
            .SingleOrDefaultAsync(e => e.Name == name);

        if (credential == null)
            throw new EntityNotFoundException($"Federated Credential with name {name} not found.");

        return credential;
    }
}
