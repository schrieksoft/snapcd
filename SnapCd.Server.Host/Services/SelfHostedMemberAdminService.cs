using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.System;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Admin;
using SnapCd.Server.Core.Services.Email;

namespace SnapCd.Server.Host.Services;

public class SelfHostedMemberAdminService(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    UserManager<User> userManager,
    ISnapCdEmailSender emailSender,
    IHttpContextAccessor httpContextAccessor,
    ILogger<SelfHostedMemberAdminService> logger) : IMemberAdminService
{
    public async Task<(bool EmailSent, string ResetLink)> ResetMemberPasswordAsync(
        Guid organizationId, Guid targetUserId, Guid actingUserId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        await AuthorizeAsync(db, organizationId, actingUserId);
        await ValidateMembershipAsync(db, organizationId, targetUserId);

        var targetUser = await userManager.FindByIdAsync(targetUserId.ToString())
            ?? throw new InvalidOperationException($"User {targetUserId} not found");

        var token = await userManager.GeneratePasswordResetTokenAsync(targetUser);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var request = httpContextAccessor.HttpContext?.Request;
        var baseUrl = request != null ? $"{request.Scheme}://{request.Host}" : "https://localhost";
        var resetLink = $"{baseUrl}/Account/ResetPassword?code={code}&email={Uri.EscapeDataString(targetUser.Email!)}";

        var emailSent = await emailSender.SendPasswordResetLinkWithOutcomeAsync(targetUser, targetUser.Email!, resetLink);

        logger.LogInformation(
            "Password reset initiated for user {TargetId} by admin {ActorId} in org {OrgId} (email delivered: {EmailSent})",
            targetUserId, actingUserId, organizationId, emailSent);

        return (EmailSent: emailSent, ResetLink: resetLink);
    }

    public async Task ForceConfirmEmailAsync(
        Guid organizationId, Guid targetUserId, Guid actingUserId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        await AuthorizeAsync(db, organizationId, actingUserId);
        await ValidateMembershipAsync(db, organizationId, targetUserId);

        var targetUser = await userManager.FindByIdAsync(targetUserId.ToString())
            ?? throw new InvalidOperationException($"User {targetUserId} not found");

        if (targetUser.EmailConfirmed) return;

        var token = await userManager.GenerateEmailConfirmationTokenAsync(targetUser);
        var result = await userManager.ConfirmEmailAsync(targetUser, token);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        logger.LogInformation(
            "Email confirmation forced for user {TargetId} by admin {ActorId} in org {OrgId}",
            targetUserId, actingUserId, organizationId);
    }

    private static async Task AuthorizeAsync(SnapCdDbContext db, Guid organizationId, Guid actingUserId)
    {
        var hasSystemAdmin = await db.Set<UserSystemRoleAssignment>()
            .AnyAsync(r => r.UserId == actingUserId && r.RoleName == SystemRole.Administrator);
        if (hasSystemAdmin) return;

        var hasOrgAdminRole = await db.UserOrganizationRoleAssignments
            .AnyAsync(r => r.OrganizationId == organizationId
                        && r.UserId == actingUserId
                        && (r.RoleName == OrganizationRole.Owner
                         || r.RoleName == OrganizationRole.IdentityAccessManager));
        if (!hasOrgAdminRole)
            throw new PrincipalNotAuthorizedException(
                $"User {actingUserId} requires SystemAdministrator, OrganizationRole.Owner, " +
                $"or OrganizationRole.IdentityAccessManager to perform this action.");
    }

    private static async Task ValidateMembershipAsync(SnapCdDbContext db, Guid organizationId, Guid targetUserId)
    {
        var isMember = await db.OrganizationUsers
            .AnyAsync(ou => ou.OrganizationId == organizationId && ou.UserId == targetUserId);
        if (!isMember)
            throw new InvalidOperationException(
                $"User {targetUserId} is not a member of organization {organizationId}");
    }
}
