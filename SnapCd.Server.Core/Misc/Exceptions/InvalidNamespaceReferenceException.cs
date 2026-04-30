namespace SnapCd.Server.Core.Misc.Exceptions;

public class InvalidNamespaceRefereceException : Exception
{
    public InvalidNamespaceRefereceException(string message) : base(message)
    {
    }

    public InvalidNamespaceRefereceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}