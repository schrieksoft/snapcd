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
using SnapCd.Contracts.Dto.Outputs;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;

public class OutputRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<OutputRepositorySettings> options)
{
    public OutputRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new OutputRepository(dbContext, principalProvider, bus, options);
    }
}

public class OutputRepository : GenericModuleGrandChildRepository<
    Output,
    OutputSet,
    OutputReadDto,
    OutputCreatedEvent,
    OutputUpdatedEvent,
    OutputDeletedEvent,
    OutputRepositorySettings>
{
    public OutputRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<OutputRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override OutputReadDto MapToDto(Output entity)
    {
        return OutputMapper.ToDto(entity);
    }

    protected override Func<Output, Guid> ParentIdAccessor => output => output.OutputSetId;

    protected override Func<SnapCdDbContext, DbSet<OutputSet>> ParentDbSetAccessor => ctx => ctx.OutputSets;

    public Task<List<Output>> ListByOutputSetIds(List<Guid> outputSetIds, Guid organizationId)
    {
        var outputs = new List<Output>();

        var literalOutputs = DbContext.LiteralOutputs
            .Include(x => x.OutputSet)
            .Where(x => outputSetIds.Contains(x.OutputSet.Id) && x.OrganizationId == organizationId)
            .ToList();

        var outputSecrets = DbContext.SecretOutputs
            .Include(x => x.OutputSet)
            .Where(x => outputSetIds.Contains(x.OutputSet.Id) && x.OrganizationId == organizationId)
            .ToList();

        outputs.AddRange(literalOutputs);
        outputs.AddRange(outputSecrets);

        return Task.FromResult(outputs);
    }

    public Task<List<Output>> ListByIds(List<Guid> outputIds, Guid organizationId)
    {
        var outputs = new List<Output>();

        var literalOutputs = DbContext.LiteralOutputs
            .Include(x => x.OutputSet)
            .Where(x => outputIds.Contains(x.Id) && x.OrganizationId == organizationId)
            .ToList();

        var outputSecrets = DbContext.SecretOutputs
            .Include(x => x.OutputSet)
            .Where(x => outputIds.Contains(x.Id) && x.OrganizationId == organizationId)
            .ToList();

        outputs.AddRange(literalOutputs);
        outputs.AddRange(outputSecrets);

        return Task.FromResult(outputs);
    }
}