namespace SnapCd.Server.Core.Services.Edition;

/// <summary>
/// Reports whether an organization is currently activated for billable use.
/// On SaaS this means the org has an active Stripe subscription and an Active
/// <c>Organization.Status</c>; on CE / SelfHosted the default implementation
/// always returns <c>true</c> (every org is activated by virtue of running on
/// the customer's own infrastructure).
/// </summary>
public interface IOrganizationActivationService
{
    Task<bool> IsActivatedAsync(Guid organizationId, CancellationToken ct = default);
}

/// <summary>
/// Default implementation used by CE / SelfHosted (and any deployment that does
/// not register a SaaS-specific override). Treats every organization as activated.
/// </summary>
public class AlwaysActivatedOrganizationActivationService : IOrganizationActivationService
{
    public Task<bool> IsActivatedAsync(Guid organizationId, CancellationToken ct = default) =>
        Task.FromResult(true);
}
