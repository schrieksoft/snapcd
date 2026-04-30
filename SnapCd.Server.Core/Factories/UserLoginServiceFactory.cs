using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.IdentityAccess;

namespace SnapCd.Server.Core.Factories;

public class UserLoginServiceFactory
{
    private readonly IServiceProvider _serviceProvider;

    public UserLoginServiceFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ScopedService<UserLoginService> Create()
    {
        var scope = _serviceProvider.CreateScope();

        // Get all required dependencies from the scoped service provider
        var service = scope.ServiceProvider.GetRequiredService<UserLoginService>();

        return new ScopedService<UserLoginService>(service, scope);
    }

    public (UserLoginService Repository, IServiceScope Scope) CreateWithScope()
    {
        var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<UserLoginService>();

        return (service, scope);
    }
}