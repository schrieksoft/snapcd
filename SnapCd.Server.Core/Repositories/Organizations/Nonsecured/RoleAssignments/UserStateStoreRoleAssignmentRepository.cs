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
using System.Linq.Expressions;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments;

public class UserStateStoreRoleAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<UserStateStoreRoleAssignmentRepositorySettings> options)
{
    public UserStateStoreRoleAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new UserStateStoreRoleAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class UserStateStoreRoleAssignmentRepository : GenericStateStoreChildRepository<UserStateStoreRoleAssignment, UserStateStoreRoleAssignmentReadDto, UserStateStoreRoleAssignmentMetadata, UserStateStoreRoleAssignmentCreatedEvent,
    UserStateStoreRoleAssignmentUpdatedEvent, UserStateStoreRoleAssignmentDeletedEvent, UserStateStoreRoleAssignmentRepositorySettings>
{

    protected override Expression<Func<UserStateStoreRoleAssignment, UserStateStoreRoleAssignmentMetadata>> MetadataProjection =>
        e => new UserStateStoreRoleAssignmentMetadata
        {
            Id = e.Id,
            OrganizationId = e.OrganizationId,
            UserId = e.UserId,
            StateStoreId = e.StateStoreId,
            PrincipalId = e.PrincipalId
        };
    public UserStateStoreRoleAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<UserStateStoreRoleAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override UserStateStoreRoleAssignmentReadDto MapToDto(UserStateStoreRoleAssignment entity)
    {
        return UserStateStoreRoleAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(UserStateStoreRoleAssignment entity)
    {
        var currentCount = await DbContext.UserStateStoreRoleAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.UserStateStoreRoleAssignmentQuota), currentCount);
    }

    public async Task<List<UserStateStoreRoleAssignment>> ListByUser(Guid userId, Guid organizationId)
    {
        return await DbContext.UserStateStoreRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.UserId == userId)
            .ToListAsync();
    }

    public async Task<List<UserStateStoreRoleAssignment>> ListByStateStore(Guid stateStoreId, Guid organizationId)
    {
        return await DbContext.UserStateStoreRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.StateStoreId == stateStoreId)
            .ToListAsync();
    }

    public async Task<List<UserStateStoreRoleAssignment>> ListByRole(StateStoreRole role, Guid organizationId)
    {
        return await DbContext.UserStateStoreRoleAssignments
            .Where(r => r.OrganizationId == organizationId && r.RoleName == role)
            .ToListAsync();
    }
}
