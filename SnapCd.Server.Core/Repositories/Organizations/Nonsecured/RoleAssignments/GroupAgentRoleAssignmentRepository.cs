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

public class GroupAgentRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<GroupAgentRoleAssignmentRepositorySettings> options)
{
    public GroupAgentRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new GroupAgentRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class GroupAgentRoleAssignmentRepository : GenericAgentChildRepository<GroupAgentRoleAssignment, GroupAgentRoleAssignmentReadDto, GroupAgentRoleAssignmentCreatedEvent,
    GroupAgentRoleAssignmentUpdatedEvent, GroupAgentRoleAssignmentDeletedEvent, GroupAgentRoleAssignmentRepositorySettings>
{
    public GroupAgentRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<GroupAgentRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override GroupAgentRoleAssignmentReadDto MapToDto(GroupAgentRoleAssignment entity)
    {
        return GroupAgentRoleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(GroupAgentRoleAssignment entity)
    {
        var currentCount = await DbContext.GroupAgentRoleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.GroupAgentRoleAssignmentQuota), currentCount);
    }

    public async Task<List<GroupAgentRoleAssignment>> ListByGroup(Guid groupId, Guid organizationId)
    {
        return await DbContext.GroupAgentRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<List<GroupAgentRoleAssignment>> ListByAgent(Guid agentId, Guid organizationId)
    {
        return await DbContext.GroupAgentRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.AgentId == agentId)
            .ToListAsync();
    }

    public async Task<List<GroupAgentRoleAssignment>> ListByRole(AgentRole role, Guid organizationId)
    {
        return await DbContext.GroupAgentRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RoleName == role)
            .ToListAsync();
    }
}
