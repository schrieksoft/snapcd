namespace SnapCd.Server.Core.Services.Admin;

/// <summary>
/// Self-Hosted-only admin operations on org members (reset password, force-confirm email).
/// Registered by the SH host; not registered on SaaS.
/// </summary>
public interface IMemberAdminService
{
    Task<(bool EmailSent, string ResetLink)> ResetMemberPasswordAsync(
        Guid organizationId, Guid targetUserId, Guid actingUserId);

    Task ForceConfirmEmailAsync(
        Guid organizationId, Guid targetUserId, Guid actingUserId);
}
