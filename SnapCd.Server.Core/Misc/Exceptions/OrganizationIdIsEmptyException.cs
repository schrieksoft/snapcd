namespace SnapCd.Server.Core.Misc.Exceptions;

public class OrganizationIdIsEmptyException : Exception
{
    public OrganizationIdIsEmptyException() : base("Organization ID cannot be empty")
    {
    }
}
