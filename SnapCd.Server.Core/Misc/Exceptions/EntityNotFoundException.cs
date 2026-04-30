namespace SnapCd.Server.Core.Misc.Exceptions;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string message = "Entity not found")
        : base(message) // Custom 4xx code for "Entity not found"
    {
    }
}