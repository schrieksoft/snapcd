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
using SnapCd.Contracts.Dto.ServicePrincipalFederatedCredentials;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Services.Interfaces.PrincipalProvider;
using SnapCd.Server.Core.Services.Nonsecured.PrincipalProvider;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ServicePrincipalFederatedCredentialSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ServicePrincipalFederatedCredentialRepositorySettings> options)
{
    public ServicePrincipalFederatedCredentialSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ServicePrincipalFederatedCredentialSecuredRepository(
            new ServicePrincipalFederatedCredentialRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

// A Federated Credential grants access as its Service Principal, so it is governed by the same
// organization roles that govern the Service Principal itself.
public class ServicePrincipalFederatedCredentialSecuredRepository : GenericOrganizationChildSecuredRepository<
    ServicePrincipalFederatedCredential,
    ServicePrincipalFederatedCredentialReadDto,
    ServicePrincipalFederatedCredentialMetadata,
    ServicePrincipalFederatedCredentialRepository,
    ServicePrincipalFederatedCredentialCreatedEvent,
    ServicePrincipalFederatedCredentialUpdatedEvent,
    ServicePrincipalFederatedCredentialDeletedEvent,
    ServicePrincipalFederatedCredentialRepositorySettings>
{
    public ServicePrincipalFederatedCredentialSecuredRepository(
        ServicePrincipalFederatedCredentialRepository repository,
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

    public Task<ServicePrincipalFederatedCredential> GetByName(string name, Guid servicePrincipalId, Guid organizationId)
    {
        return Repository.GetByName(name, servicePrincipalId, organizationId);
    }
}
