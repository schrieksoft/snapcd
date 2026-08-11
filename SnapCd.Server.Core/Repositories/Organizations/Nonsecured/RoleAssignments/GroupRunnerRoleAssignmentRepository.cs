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
using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments;

public class GroupRunnerRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<GroupRunnerRoleAssignmentRepositorySettings> options)
{
    public GroupRunnerRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new GroupRunnerRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class GroupRunnerRoleAssignmentRepository : GenericRunnerChildRepository<GroupRunnerRoleAssignment, GroupRunnerRoleAssignmentReadDto, GroupRunnerRoleAssignmentCreatedEvent,
    GroupRunnerRoleAssignmentUpdatedEvent, GroupRunnerRoleAssignmentDeletedEvent, GroupRunnerRoleAssignmentRepositorySettings>
{
    public GroupRunnerRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<GroupRunnerRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override GroupRunnerRoleAssignmentReadDto MapToDto(GroupRunnerRoleAssignment entity)
    {
        return GroupRunnerRoleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(GroupRunnerRoleAssignment entity)
    {
        var currentCount = await DbContext.GroupRunnerRoleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.GroupRunnerRoleAssignmentQuota), currentCount);
    }

    public async Task<List<GroupRunnerRoleAssignment>> ListByGroup(Guid groupId, Guid organizationId)
    {
        return await DbContext.GroupRunnerRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<List<GroupRunnerRoleAssignment>> ListByRunner(Guid runnerId, Guid organizationId)
    {
        return await DbContext.GroupRunnerRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RunnerId == runnerId)
            .ToListAsync();
    }

    public async Task<List<GroupRunnerRoleAssignment>> ListByRole(RunnerRole role, Guid organizationId)
    {
        return await DbContext.GroupRunnerRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RoleName == role)
            .ToListAsync();
    }
}