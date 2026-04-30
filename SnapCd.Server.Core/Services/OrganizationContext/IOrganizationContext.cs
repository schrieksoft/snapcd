namespace SnapCd.Server.Core.Services.OrganizationContext;

public interface IOrganizationContext
{
    Guid? CurrentOrganizationId { get; }
}
