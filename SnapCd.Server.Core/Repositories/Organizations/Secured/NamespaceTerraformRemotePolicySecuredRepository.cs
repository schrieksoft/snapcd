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
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Contracts.Dto.NamespaceTerraformRemotePolicies;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Views;
using System.Linq.Expressions;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class NamespaceTerraformRemotePolicySecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<NamespaceTerraformRemotePolicyRepositorySettings> options)
{
    public NamespaceTerraformRemotePolicySecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceTerraformRemotePolicySecuredRepository(
            new NamespaceTerraformRemotePolicyRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class NamespaceTerraformRemotePolicySecuredRepository : GenericNamespaceChildSecuredRepository<
    NamespaceTerraformRemotePolicy,
    NamespaceTerraformRemotePolicyReadDto, NamespaceTerraformRemotePolicyMetadata,
    NamespaceTerraformRemotePolicyRepository,
    NamespaceTerraformRemotePolicyCreatedEvent,
    NamespaceTerraformRemotePolicyUpdatedEvent,
    NamespaceTerraformRemotePolicyDeletedEvent,
    NamespaceTerraformRemotePolicyRepositorySettings>
{
    public NamespaceTerraformRemotePolicySecuredRepository(
        NamespaceTerraformRemotePolicyRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<NamespaceTerraformRemotePolicy> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await Repository.Get(namespaceId, name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(NamespaceTerraformRemotePolicy)} with ID {entity.Id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return entity;
    }

    public Task<NamespaceTerraformRemotePolicyMetadata> GetMetadata(Guid namespaceId, string name, Guid organizationId)
        => GetMetadataByKey(
            organizationId,
            async () => (await Repository.Get(namespaceId, name, organizationId)).Id,
            id => Repository.GetMetadata(id, organizationId),
            $"name \"{name}\"");
}
