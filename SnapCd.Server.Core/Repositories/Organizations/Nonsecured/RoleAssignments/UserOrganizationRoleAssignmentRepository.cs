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

public class UserOrganizationRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<UserOrganizationRoleAssignmentRepositorySettings> options)
{
    public UserOrganizationRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new UserOrganizationRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class UserOrganizationRoleAssignmentRepository : GenericOrganizationChildRepository<UserOrganizationRoleAssignment, UserOrganizationRoleAssignmentReadDto, UserOrganizationRoleAssignmentCreatedEvent
    , UserOrganizationRoleAssignmentUpdatedEvent, UserOrganizationRoleAssignmentDeletedEvent, UserOrganizationRoleAssignmentRepositorySettings>
{
    public UserOrganizationRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<UserOrganizationRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override UserOrganizationRoleAssignmentReadDto MapToDto(UserOrganizationRoleAssignment entity)
    {
        return UserOrganizationRoleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(UserOrganizationRoleAssignment entity)
    {
        var currentCount = await DbContext.UserOrganizationRoleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.UserOrganizationRoleAssignmentQuota), currentCount);
    }

    public async Task<List<UserOrganizationRoleAssignment>> ListByUser(Guid userId, Guid organizationId)
    {
        return await DbContext.UserOrganizationRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<UserOrganizationRoleAssignment>> ListByRole(OrganizationRole role, Guid organizationId)
    {
        return await DbContext.UserOrganizationRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RoleName == role)
            .ToListAsync();
    }
}