// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services;

public class QuotaEnforcementService(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    QuotaService quotaService,
    ILogger<QuotaEnforcementService> logger)
{
    private static readonly OrganizationRole[] ProtectedRoles =
        [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager];

    /// <summary>
    /// Enforces user quota by deactivating excess users.
    /// Protected users (with Owner or IdentityAccessManager role, directly or via groups) are never deactivated.
    /// Among unprotected users, newest are deactivated first (preserves longest-tenured users).
    /// </summary>
    public async Task EnforceUserQuotaAsync(Guid organizationId)
    {
        var quota = await quotaService.GetQuotaAsync(organizationId, nameof(QuotaLimits.OrganizationUserQuota));
        if (quota == null)
        {
            logger.LogDebug("No user quota configured for organization {OrgId}, skipping enforcement", organizationId);
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        // Get all active users
        var activeUsers = await dbContext.OrganizationUsers
            .Where(ou => ou.OrganizationId == organizationId && !ou.IsDeactivated)
            .ToListAsync();

        if (activeUsers.Count <= quota.Value)
        {
            logger.LogDebug(
                "Organization {OrgId} user count {Count} is within quota {Quota}, no enforcement needed",
                organizationId, activeUsers.Count, quota.Value);
            return;
        }

        var excessCount = activeUsers.Count - quota.Value;

        // Find users with protected roles (directly assigned)
        var usersWithDirectRole = await dbContext.UserOrganizationRoleAssignments
            .Where(ra => ra.OrganizationId == organizationId && ProtectedRoles.Contains(ra.RoleName))
            .Select(ra => ra.UserId)
            .ToListAsync();

        // Find users with protected roles via group membership (including nested groups)
        var usersWithGroupRole = await (
            from gum in dbContext.UserGroupMembers
            where gum.OrganizationId == organizationId
            join rgm in dbContext.RecursiveGroupMembers
                on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in dbContext.GroupOrganizationRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                equals new { assignment.OrganizationId, GroupId = assignment.PrincipalId }
            where ProtectedRoles.Contains(assignment.RoleName)
            select gum.UserId
        ).ToListAsync();

        var protectedUserIds = usersWithDirectRole.Union(usersWithGroupRole).ToHashSet();

        // Separate protected and unprotected users
        var protectedUsers = activeUsers.Where(u => protectedUserIds.Contains(u.UserId)).ToList();
        var unprotectedUsers = activeUsers
            .Where(u => !protectedUserIds.Contains(u.UserId))
            .OrderByDescending(u => u.CreatedDateTime) // Newest first (to deactivate)
            .ToList();

        // Deactivate newest unprotected users first
        var usersToDeactivate = unprotectedUsers.Take(excessCount).ToList();

        if (usersToDeactivate.Count < excessCount)
        {
            logger.LogWarning(
                "Organization {OrgId} has {Excess} excess users but only {Available} can be deactivated. " +
                "Protected users (Owner/IdentityAccessManager): {Protected}",
                organizationId, excessCount, usersToDeactivate.Count, protectedUsers.Count);
        }

        foreach (var user in usersToDeactivate)
        {
            user.IsDeactivated = true;
            logger.LogInformation(
                "Deactivated user {UserId} in organization {OrgId} due to quota enforcement",
                user.UserId, organizationId);
        }

        if (usersToDeactivate.Count > 0)
        {
            await dbContext.SaveChangesAsync();
            logger.LogInformation(
                "Quota enforcement complete for organization {OrgId}: deactivated {Count} users",
                organizationId, usersToDeactivate.Count);
        }
    }

    /// <summary>
    /// Checks if module job creation is allowed based on quota.
    /// Jobs are blocked when the organization's module count exceeds its quota.
    /// </summary>
    public async Task<(bool Allowed, string? Reason)> CanCreateModuleJobAsync(Guid organizationId)
    {
        var quota = await quotaService.GetQuotaAsync(organizationId, nameof(QuotaLimits.ModuleQuota));
        if (quota == null)
        {
            return (true, null); // Unlimited
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var moduleCount = await dbContext.Modules
            .CountAsync(m => m.OrganizationId == organizationId);

        if (moduleCount > quota.Value)
        {
            var reason = $"Module quota exceeded. Current: {moduleCount}, Limit: {quota.Value}. " +
                         "Delete modules or upgrade subscription to run jobs.";
            return (false, reason);
        }

        return (true, null);
    }
}
