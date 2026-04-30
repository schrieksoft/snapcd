namespace SnapCd.Server.Core.Misc.Exceptions;

public class PrincipalNotAuthorizedException : Exception
{
    public PrincipalNotAuthorizedException(string message = "Principal does not have permission")
        : base(message)
    {
    }
}