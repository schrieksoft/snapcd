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

public class GroupOrganizationRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<GroupOrganizationRoleAssignmentRepositorySettings> options)
{
    public GroupOrganizationRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new GroupOrganizationRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class GroupOrganizationRoleAssignmentRepository : GenericOrganizationChildRepository<GroupOrganizationRoleAssignment, GroupOrganizationRoleAssignmentReadDto,
    GroupOrganizationRoleAssignmentCreatedEvent, GroupOrganizationRoleAssignmentUpdatedEvent, GroupOrganizationRoleAssignmentDeletedEvent, GroupOrganizationRoleAssignmentRepositorySettings>
{
    public GroupOrganizationRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<GroupOrganizationRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override GroupOrganizationRoleAssignmentReadDto MapToDto(GroupOrganizationRoleAssignment entity)
    {
        return GroupOrganizationRoleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(GroupOrganizationRoleAssignment entity)
    {
        var currentCount = await DbContext.GroupOrganizationRoleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.GroupOrganizationRoleAssignmentQuota), currentCount);
    }

    public async Task<List<GroupOrganizationRoleAssignment>> ListByGroup(Guid groupId, Guid organizationId)
    {
        return await DbContext.GroupOrganizationRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<List<GroupOrganizationRoleAssignment>> ListByRole(OrganizationRole role, Guid organizationId)
    {
        return await DbContext.GroupOrganizationRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RoleName == role)
            .ToListAsync();
    }
}