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
using SnapCd.Contracts.Dto.Secrets.Scoped;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.Secrets.Scoped;

public class NamespaceSecretSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<NamespaceSecretRepositorySettings> options)
{
    public NamespaceSecretSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceSecretSecuredRepository(
            new NamespaceSecretRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class NamespaceSecretSecuredRepository : GenericNamespaceChildSecuredRepository<
    NamespaceSecret,
    NamespaceSecretDto,
    NamespaceSecretRepository,
    NamespaceSecretCreatedEvent,
    NamespaceSecretUpdatedEvent,
    NamespaceSecretDeletedEvent,
    NamespaceSecretRepositorySettings>
{
    public NamespaceSecretSecuredRepository(
        NamespaceSecretRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<NamespaceSecret> GetByName(string name, Guid organizationId,
        Func<IQueryable<NamespaceSecret>, IQueryable<NamespaceSecret>>? include = null)
    {
        var secret = await Repository.GetByName(name, include);

        if (!CanRead(secret.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to NamespaceSecret {secret.Id}");

        return secret;
    }

    public async Task<List<NamespaceSecret>> ListByIds(List<Guid> ids, Guid organizationId)
    {
        var secrets = await Repository.ListByIds(ids, organizationId);

        foreach (var secret in secrets)
            if (!CanRead(secret.Id, organizationId))
                throw new UnauthorizedAccessException($"Access denied to NamespaceSecret {secret.Id}");

        return secrets;
    }
}