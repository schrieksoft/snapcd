namespace SnapCd.Server.Core.Misc.Exceptions;

public class IdIsEmptyException : Exception
{
    public IdIsEmptyException(string message = "Entity ID cannot be empty")
        : base(message)
    {
    }
}