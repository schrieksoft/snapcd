// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.System;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Services.OrganizationContext;

public class OrganizationMembershipCacheService
{
    private readonly IDistributedCache _cache;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public OrganizationMembershipCacheService(
        IDistributedCache cache,
        IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _cache = cache;
        _dbContextFactory = dbContextFactory;
    }

    public async Task<bool> IsActiveMemberAsync(Guid userId, Guid organizationId)
    {
        var cacheKey = MembershipCacheKey(userId, organizationId);

        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
            return cached == "1";

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var isMember = await dbContext.OrganizationUsers
            .AnyAsync(ou => ou.UserId == userId
                            && ou.OrganizationId == organizationId
                            && !ou.IsDeactivated
                            && ou.InvitationCompleted);

        await _cache.SetStringAsync(cacheKey, isMember ? "1" : "0", CacheOptions);

        return isMember;
    }

    public async Task<HashSet<OrganizationRole>> GetOrganizationRolesAsync(Guid userId, Guid organizationId)
    {
        var cacheKey = RolesCacheKey(userId, organizationId);
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached is not null)
        {
            return cached.Length == 0
                ? new HashSet<OrganizationRole>()
                : cached.Split(',').Select(s => Enum.Parse<OrganizationRole>(s)).ToHashSet();
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var directRoles = await dbContext.UserOrganizationRoleAssignments
            .Where(ra => ra.OrganizationId == organizationId && ra.UserId == userId)
            .Select(ra => ra.RoleName)
            .ToListAsync();

        // RecursiveGroupMembers can walk across organizations (a parent group in one org
        // may contain a child group in another). For a per-org role check we must keep
        // the resolved role assignment in the org we are checking — otherwise roles held
        // on a different org leak in.
        var groupRoles = await (
            from gum in dbContext.UserGroupMembers
            where gum.OrganizationId == organizationId && gum.UserId == userId
            join rgm in dbContext.RecursiveGroupMembers
                on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in dbContext.GroupOrganizationRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                equals new { assignment.OrganizationId, GroupId = assignment.PrincipalId }
            where assignment.OrganizationId == organizationId
            select assignment.RoleName
        ).ToListAsync();

        var roles = directRoles.Concat(groupRoles).ToHashSet();
        await _cache.SetStringAsync(cacheKey, string.Join(",", roles.Select(r => r.ToString())), CacheOptions);
        return roles;
    }

    public async Task<bool> IsSystemAdministratorAsync(Guid userId)
    {
        var cacheKey = SysAdminCacheKey(userId);
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached is not null) return cached == "1";

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var isAdmin = await dbContext.Set<UserSystemRoleAssignment>()
            .AnyAsync(r => r.UserId == userId && r.RoleName == SystemRole.Administrator);

        await _cache.SetStringAsync(cacheKey, isAdmin ? "1" : "0", CacheOptions);
        return isAdmin;
    }

    public async Task<bool> HasAnyOrganizationRoleAsync(
        Guid userId, Guid organizationId, IReadOnlyCollection<OrganizationRole> anyOf)
    {
        if (anyOf.Count == 0) return true;
        if (await IsSystemAdministratorAsync(userId)) return true;
        var roles = await GetOrganizationRolesAsync(userId, organizationId);
        return anyOf.Any(roles.Contains);
    }

    public async Task InvalidateAsync(Guid userId, Guid organizationId)
    {
        await _cache.RemoveAsync(MembershipCacheKey(userId, organizationId));
        await _cache.RemoveAsync(RolesCacheKey(userId, organizationId));
    }

    private static string MembershipCacheKey(Guid userId, Guid organizationId)
        => $"org-membership:{userId}:{organizationId}";

    private static string RolesCacheKey(Guid userId, Guid organizationId)
        => $"org-roles:{userId}:{organizationId}";

    private static string SysAdminCacheKey(Guid userId)
        => $"sys-admin:{userId}";
}
