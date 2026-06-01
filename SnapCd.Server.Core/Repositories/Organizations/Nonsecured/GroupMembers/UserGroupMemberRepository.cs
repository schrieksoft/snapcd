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
using SnapCd.Contracts.Dto.GroupMembers;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers.GroupMembers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.GroupMembers;

public class UserGroupMemberRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<UserGroupMemberRepositorySettings> options)
{
    public UserGroupMemberRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new UserGroupMemberRepository(dbContext, principalProvider, bus, options);
    }
}

public class UserGroupMemberRepository : GenericOrganizationChildRepository<UserGroupMember, UserGroupMemberReadDto, UserGroupMemberCreatedEvent, UserGroupMemberUpdatedEvent, UserGroupMemberDeletedEvent,
    UserGroupMemberRepositorySettings>
{
    public UserGroupMemberRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<UserGroupMemberRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override UserGroupMemberReadDto MapToDto(UserGroupMember entity)
    {
        return UserGroupMemberMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(UserGroupMember entity)
    {
        var currentCount = await DbContext.UserGroupMembers
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.UserGroupMemberQuota), currentCount);
    }

    public async Task<List<UserGroupMember>> ListByGroupId(Guid groupId, Guid organizationId, IQueryable<UserGroupMember>? query = null)
    {
        query ??= DbContext.Set<UserGroupMember>();

        query = query.Where(gm => gm.OrganizationId == organizationId && gm.GroupId == groupId);

        return await query.ToListAsync();
    }

    public async Task<UserGroupMember> GetByParents(Guid groupId, Guid userId, Guid organizationId)
    {
        var userGroupMember = await DbContext.UserGroupMembers
            .SingleOrDefaultAsync(i => i.GroupId == groupId && i.UserId == userId && i.OrganizationId == organizationId);

        if (userGroupMember == null)
            throw new EntityNotFoundException(
                $"UserGroupMember with GroupId {groupId} and UserId {userId} not found.");

        return userGroupMember;
    }
}