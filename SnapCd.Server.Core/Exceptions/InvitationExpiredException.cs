namespace SnapCd.Server.Core.Exceptions;

public class InvitationExpiredException : Exception
{
    public DateTime ExpirationDateTime { get; }

    public InvitationExpiredException(DateTime expirationDateTime)
        : base($"This invitation expired on {expirationDateTime:yyyy-MM-dd}. Please request a new invitation.")
    {
        ExpirationDateTime = expirationDateTime;
    }

    public InvitationExpiredException(string message) : base(message)
    {
        ExpirationDateTime = DateTime.MinValue;
    }
}
