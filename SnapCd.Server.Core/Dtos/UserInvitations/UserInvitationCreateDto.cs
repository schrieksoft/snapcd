namespace SnapCd.Server.Core.Dtos.UserInvitations;

public class UserInvitationCreateDto
{
    public string Email { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public Guid? UsedByUserId { get; set; }
    public string? UsedByUserName { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
}
