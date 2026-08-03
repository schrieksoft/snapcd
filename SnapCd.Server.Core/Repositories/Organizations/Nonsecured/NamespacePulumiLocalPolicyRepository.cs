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
using SnapCd.Contracts.Dto.NamespacePulumiLocalPolicies;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class NamespacePulumiLocalPolicyRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<NamespacePulumiLocalPolicyRepositorySettings> options)
{
    public NamespacePulumiLocalPolicyRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespacePulumiLocalPolicyRepository(dbContext, principalProvider, bus, options);
    }
}

public class NamespacePulumiLocalPolicyRepository : GenericNamespaceChildDefinitionRepository<NamespacePulumiLocalPolicy, NamespacePulumiLocalPolicyReadDto, NamespacePulumiLocalPolicyCreatedEvent, NamespacePulumiLocalPolicyUpdatedEvent,
    NamespacePulumiLocalPolicyDeletedEvent, NamespacePulumiLocalPolicyRepositorySettings>
{
    public NamespacePulumiLocalPolicyRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<NamespacePulumiLocalPolicyRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override NamespacePulumiLocalPolicyReadDto MapToDto(NamespacePulumiLocalPolicy entity)
    {
        return NamespacePulumiLocalPolicyMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(NamespacePulumiLocalPolicy entity)
    {
        var currentCount = await DbContext.NamespacePulumiLocalPolicies
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.NamespacePulumiLocalPolicyQuota), currentCount);
    }

    public async Task<NamespacePulumiLocalPolicy> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await DbContext.NamespacePulumiLocalPolicies
            .SingleOrDefaultAsync(e => e.Name == name && e.NamespaceId == namespaceId && e.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException($"NamespacePulumiLocalPolicy with name {name} not found for namespace {namespaceId}.");

        return entity;
    }
}
