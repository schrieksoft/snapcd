namespace SnapCd.Server.Core.Exceptions;

public class EmailVerificationRequiredException : Exception
{
    public EmailVerificationRequiredException()
        : base("Please verify your email address before sending invitations.")
    {
    }

    public EmailVerificationRequiredException(string message) : base(message)
    {
    }
}
