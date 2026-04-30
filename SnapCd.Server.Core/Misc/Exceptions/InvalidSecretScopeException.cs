namespace SnapCd.Server.Core.Misc.Exceptions;

public class InvalidSecretScopeException : Exception
{
    public InvalidSecretScopeException(string message) : base(message)
    {
    }

    public InvalidSecretScopeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}