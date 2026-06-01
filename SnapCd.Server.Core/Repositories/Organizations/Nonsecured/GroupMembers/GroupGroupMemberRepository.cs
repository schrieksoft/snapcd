// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Data;
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

public class GroupGroupMemberRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<GroupGroupMemberRepositorySettings> options)
{
    public GroupGroupMemberRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new GroupGroupMemberRepository(dbContext, principalProvider, bus, options);
    }
}

public class GroupGroupMemberRepository : GenericOrganizationChildRepository<GroupGroupMember, GroupGroupMemberReadDto, GroupGroupMemberCreatedEvent, GroupGroupMemberUpdatedEvent,
    GroupGroupMemberDeletedEvent, GroupGroupMemberRepositorySettings>
{
    private const int MaxGroupHierarchyDepth = 10;

    public GroupGroupMemberRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<GroupGroupMemberRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override GroupGroupMemberReadDto MapToDto(GroupGroupMember entity)
    {
        return GroupGroupMemberMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(GroupGroupMember entity)
    {
        var currentCount = await DbContext.GroupGroupMembers
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.GroupGroupMemberQuota), currentCount);
    }

    protected override async Task<GroupGroupMember> CreateInTransaction(GroupGroupMember entity)
    {
        // Use serializable isolation to prevent concurrent modifications from creating cycles/depth issues
        await using var transaction = await DbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var result = await ExecuteCreate(entity);
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public override async Task<GroupGroupMember> ExecuteCreate(GroupGroupMember entity)
    {
        // Validate before creation
        await ValidateGroupOrganizationUser(entity.GroupId, entity.MemberGroupId, entity.OrganizationId);

        // Execute the base create operation
        var result = await base.ExecuteCreate(entity);

        // Verify integrity after save (defense in depth)
        await VerifyNoCircularReference(entity.MemberGroupId, entity.OrganizationId);
        await VerifyMaxDepthNotExceeded(entity.OrganizationId);

        return result;
    }

    protected override async Task<GroupGroupMember> UpdateInTransaction(GroupGroupMember entity)
    {
        // Use serializable isolation to prevent concurrent modifications
        await using var transaction = await DbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var result = await ExecuteUpdate(entity);
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public override async Task<GroupGroupMember> ExecuteUpdate(GroupGroupMember entity)
    {
        // Get the existing entity to check if parent/member groups changed
        var existing = await DbContext.GroupGroupMembers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == entity.Id && x.OrganizationId == entity.OrganizationId);

        if (existing == null)
            throw new EntityNotFoundException($"GroupGroupMember with Id {entity.Id} not found.");

        // If the group relationships changed, validate
        if (existing.GroupId != entity.GroupId || existing.MemberGroupId != entity.MemberGroupId) await ValidateGroupOrganizationUser(entity.GroupId, entity.MemberGroupId, entity.OrganizationId);

        // Execute the base update operation
        var result = await base.ExecuteUpdate(entity);

        // Verify integrity after save
        await VerifyNoCircularReference(entity.MemberGroupId, entity.OrganizationId);
        await VerifyMaxDepthNotExceeded(entity.OrganizationId);

        return result;
    }

    public async Task<List<GroupGroupMember>> ListByGroupId(Guid groupId, Guid organizationId, IQueryable<GroupGroupMember>? query = null)
    {
        query ??= DbContext.Set<GroupGroupMember>();

        query = query.Where(gm => gm.OrganizationId == organizationId && gm.GroupId == groupId);

        return await query.ToListAsync();
    }

    public async Task<GroupGroupMember> GetByGroupIds(Guid parentGroupId, Guid memberGroupId, Guid organizationId)
    {
        var groupGroupMember = await DbContext.GroupGroupMembers
            .SingleOrDefaultAsync(i => i.GroupId == parentGroupId && i.MemberGroupId == memberGroupId && i.OrganizationId == organizationId);

        if (groupGroupMember == null)
            throw new EntityNotFoundException(
                $"GroupGroupMember with ParentGroupId {parentGroupId} and MemberGroupId {memberGroupId} not found.");

        return groupGroupMember;
    }

    /// <summary>
    /// Validates that adding memberGroupId to parentGroupId would not create a cycle or exceed max depth.
    /// </summary>
    private async Task ValidateGroupOrganizationUser(Guid parentGroupId, Guid memberGroupId, Guid organizationId)
    {
        // Cannot add a group as a member of itself
        if (parentGroupId == memberGroupId) throw new InvalidOperationException("A group cannot be a member of itself.");

        // Check if this would create a circular reference
        // If parentGroupId is (transitively) a member of memberGroupId, adding this organizationUser would create a cycle
        var wouldCreateCycle = await DbContext.RecursiveGroupMembers
            .AnyAsync(rgm =>
                rgm.RootGroupId == memberGroupId
                && rgm.GroupId == parentGroupId
                && rgm.OrganizationId == organizationId);

        if (wouldCreateCycle)
            throw new InvalidOperationException(
                $"Cannot add group {memberGroupId} as a member of {parentGroupId}: this would create a circular reference.");

        // Check if this would exceed maximum depth
        // Get the deepest member chain below memberGroupId (how deep does memberGroupId's tree go?)
        var maxDepthBelowMember = await DbContext.RecursiveGroupMembers
            .Where(rgm => rgm.RootGroupId == memberGroupId && rgm.OrganizationId == organizationId)
            .Select(rgm => rgm.Depth)
            .OrderByDescending(d => d)
            .FirstOrDefaultAsync();

        // Get the deepest parent chain above parentGroupId (how deep is parentGroupId nested?)
        var maxDepthAboveParent = await DbContext.RecursiveGroupMembers
            .Where(rgm => rgm.RootGroupId == parentGroupId && rgm.OrganizationId == organizationId)
            .Select(rgm => rgm.Depth)
            .OrderByDescending(d => d)
            .FirstOrDefaultAsync();

        // Combined depth: depth above parent + 1 (the new link) + depth below member
        var combinedDepth = maxDepthAboveParent + 1 + maxDepthBelowMember;

        if (combinedDepth >= MaxGroupHierarchyDepth)
            throw new InvalidOperationException(
                $"Cannot add group {memberGroupId} as a member of {parentGroupId}: this would exceed the maximum hierarchy depth of {MaxGroupHierarchyDepth}. " +
                $"Current depths: {maxDepthAboveParent} above parent, {maxDepthBelowMember} below member.");
    }

    /// <summary>
    /// Verifies that no circular reference exists starting from the given group.
    /// This is a post-save verification for defense in depth.
    /// </summary>
    private async Task VerifyNoCircularReference(Guid groupId, Guid organizationId)
    {
        // Check if the group appears in its own transitive organizationUser chain (excluding depth 0, which is itself)
        var hasCycle = await DbContext.RecursiveGroupMembers
            .AnyAsync(rgm =>
                rgm.RootGroupId == groupId
                && rgm.GroupId == groupId
                && rgm.Depth > 0
                && rgm.OrganizationId == organizationId);

        if (hasCycle)
            throw new InvalidOperationException(
                $"Circular reference detected in group hierarchy for group {groupId}. Transaction will be rolled back.");
    }

    /// <summary>
    /// Verifies that no group hierarchy exceeds the maximum depth.
    /// This is a post-save verification for defense in depth.
    /// </summary>
    private async Task VerifyMaxDepthNotExceeded(Guid organizationId)
    {
        var maxDepth = await DbContext.RecursiveGroupMembers
            .Where(rgm => rgm.OrganizationId == organizationId)
            .Select(rgm => rgm.Depth)
            .OrderByDescending(d => d)
            .FirstOrDefaultAsync();

        if (maxDepth >= MaxGroupHierarchyDepth)
            throw new InvalidOperationException(
                $"Maximum group hierarchy depth of {MaxGroupHierarchyDepth} exceeded (current: {maxDepth}). Transaction will be rolled back.");
    }
}