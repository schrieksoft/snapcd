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
using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Runner.Base;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers.RoleAssignments.Base;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments.Base;

public class RunnerRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<RunnerRoleAssignmentRepositorySettings> options)
{
    public RunnerRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new RunnerRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class RunnerRoleAssignmentRepository : GenericRunnerChildRepository<RunnerRoleAssignment, RunnerRoleAssignmentReadDto, RunnerRoleAssignmentCreatedEvent,
    RunnerRoleAssignmentUpdatedEvent, RunnerRoleAssignmentDeletedEvent, RunnerRoleAssignmentRepositorySettings>
{
    public RunnerRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<RunnerRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override RunnerRoleAssignmentReadDto MapToDto(RunnerRoleAssignment entity)
    {
        return RunnerRoleAssignmentMapper.ToDto(entity);
    }

    public override async Task<RunnerRoleAssignment> ExecuteCreate(RunnerRoleAssignment entity)
    {
        throw new NotImplementedByDesignException("RunnerRoleAssignmentRepository can only be used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }

    public override async Task<RunnerRoleAssignment> ExecuteUpdate(RunnerRoleAssignment entity)
    {
        throw new NotImplementedByDesignException("RunnerRoleAssignmentRepository can only be used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }
}