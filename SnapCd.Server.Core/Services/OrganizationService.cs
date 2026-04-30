using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.System.Nonsecured;
using SnapCd.Server.Core.Repositories.System.Secured;
using SnapCd.Server.Core.Services.IdentityAccess;

namespace SnapCd.Server.Core.Services;

public class OrganizationServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public OrganizationServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates a new OrganizationService instance within its own service scope.
    /// This ensures proper disposal of scoped dependencies like SnapCdDbContext.
    /// </summary>
    /// <returns>A ScopedService wrapper containing the OrganizationService and its scope</returns>
    public ScopedService<OrganizationService> Create()
    {
        var scope = _serviceProvider.CreateScope();

        // Get all required dependencies from the scoped service provider
        var organizationService = scope.ServiceProvider.GetRequiredService<OrganizationService>();

        return new ScopedService<OrganizationService>(organizationService, scope);
    }

    /// <summary>
    /// Creates a new OrganizationService instance within its own service scope and returns both the service and the scope.
    /// Use this when you need to manually manage the scope lifetime.
    /// </summary>
    /// <returns>A tuple containing the OrganizationService and its associated IServiceScope</returns>
    public (OrganizationService Service, IServiceScope Scope) CreateWithScope()
    {
        var scope = _serviceProvider.CreateScope();
        var organizationService = scope.ServiceProvider.GetRequiredService<OrganizationService>();

        return (organizationService, scope);
    }
}

public class OrganizationService : IDisposable
{
    private readonly OrganizationSystemRepository _organizationSystemRepository;
    private readonly OrganizationUserRepository _organizationUserRepository;
    private readonly OrganizationSecuredRepositoryFactory _organizationSecuredRepositoryFactory;

    public OrganizationService(
        OrganizationSystemRepository organizationSystemRepository,
        OrganizationUserRepository organizationUserRepository,
        OrganizationSecuredRepositoryFactory organizationSecuredRepositoryFactory
    )
    {
        _organizationSystemRepository = organizationSystemRepository;
        _organizationUserRepository = organizationUserRepository;
        _organizationSecuredRepositoryFactory = organizationSecuredRepositoryFactory;
    }


    public async Task<Organization> CreateOrganizationAsync(string name, Guid createdByUserId)
    {
        // Validate input parameters
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name cannot be null or empty.", nameof(name));

        return await _organizationSystemRepository.CreateWithOwner(name, createdByUserId);
    }

    public async Task<IEnumerable<Organization>> GetUserOrganizationsAsync(Guid userId)
    {
        // Get all organizations where the user is a member
        var organizationUsers = await _organizationUserRepository.GetByUserIdAsync(userId);
        var organizationIds = organizationUsers.Select(ou => ou.OrganizationId).ToList();

        var organizations = new List<Organization>();
        foreach (var orgId in organizationIds)
        {
            var org = await _organizationSystemRepository.Get(orgId);
            if (org != null && org.Status == OrganizationStatus.Active) organizations.Add(org);
        }

        return organizations.OrderBy(o => o.Name);
    }

    public async Task<Organization?> GetOrganizationAsync(Guid organizationId)
    {
        using var securedRepo = _organizationSecuredRepositoryFactory.Create();
        return await securedRepo.Get(organizationId);
    }

    public bool UserHasOrganizationMembership(string userId)
    {
        if (!Guid.TryParse(userId, out var userGuid))
            return false;

        return _organizationUserRepository.UserHasOrganizationMembership(userId);
    }

    public void Dispose()
    {
        // No resources to dispose - repositories are managed by DI container
    }
}