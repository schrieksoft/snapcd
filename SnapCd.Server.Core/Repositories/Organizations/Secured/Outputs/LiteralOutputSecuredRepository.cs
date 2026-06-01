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
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.Outputs;

public class LiteralOutputSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<OutputRepositorySettings> options)
{
    public LiteralOutputSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new LiteralOutputSecuredRepository(
            new LiteralOutputRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class LiteralOutputSecuredRepository : OutputSecuredRepository
{
    private new LiteralOutputRepository Repository => (LiteralOutputRepository)base.Repository;

    public LiteralOutputSecuredRepository(
        LiteralOutputRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [],
        StackRoles = [],
        NamespaceRoles = [],
        ModuleRoles = []
    };

    public override bool CanDelete(Guid id, Guid organizationId)
    {
        // LiteralOutput cannot be deleted directly
        return false;
    }

    public new async Task<List<LiteralOutput>> ListByIds(List<Guid> ids, Guid organizationId)
    {
        var outputs = await Repository.ListByIds(ids, organizationId);

        foreach (var output in outputs)
            if (!CanRead(output.Id, organizationId))
                throw new UnauthorizedAccessException($"Access denied to LiteralOutput {output.Id}");

        return outputs;
    }
}