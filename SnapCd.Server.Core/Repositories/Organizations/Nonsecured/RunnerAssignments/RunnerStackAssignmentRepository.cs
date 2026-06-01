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
using SnapCd.Contracts.Dto.RunnerStackAssignments;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerAssignments;

public class RunnerStackAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<RunnerStackAssignmentRepositorySettings> options)
{
    public RunnerStackAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new RunnerStackAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class RunnerStackAssignmentRepository : GenericOrganizationChildRepository<RunnerStackAssignment, RunnerStackAssignmentReadDto, RunnerStackAssignmentCreatedEvent,
    RunnerStackAssignmentUpdatedEvent, RunnerStackAssignmentDeletedEvent, RunnerStackAssignmentRepositorySettings>
{
    public RunnerStackAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<RunnerStackAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override RunnerStackAssignmentReadDto MapToDto(RunnerStackAssignment entity)
    {
        return RunnerStackAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(RunnerStackAssignment entity)
    {
        var currentCount = await DbContext.RunnerStackAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.RunnerStackAssignmentQuota), currentCount);
    }

    public async Task<List<RunnerStackAssignment>> ListByRunner(Guid runnerId, Guid organizationId)
    {
        return await DbContext.RunnerStackAssignments
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.RunnerId == runnerId)
            .ToListAsync();
    }

    public async Task<List<RunnerStackAssignment>> ListByStack(Guid stackId, Guid organizationId)
    {
        return await DbContext.RunnerStackAssignments
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.StackId == stackId)
            .ToListAsync();
    }
}