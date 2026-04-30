namespace SnapCd.Server.Core.Dtos.OrganizationUsers;

public class OrganizationUserCreateDto
{
    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public bool IsDeactivated { get; set; }

    public string? InvitationToken { get; set; }

    public DateTime? InvitationSentDateTime { get; set; }

    public DateTime? InvitationExpirationDateTime { get; set; }

    public bool InvitationCompleted { get; set; }

    public DateTime? InvitationCompletedDateTime { get; set; }
}
