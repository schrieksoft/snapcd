using Microsoft.AspNetCore.Identity;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.IdentityAccess;

public class UserManagerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public UserManagerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ScopedService<UserManager<User>> Create()
    {
        var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        return new ScopedService<UserManager<User>>(userManager, scope);
    }
}

public class ScopedService<T> : IDisposable where T : class
{
    private readonly IServiceScope _scope;

    public T Service { get; }

    public ScopedService(T service, IServiceScope scope)
    {
        Service = service;
        _scope = scope;
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}