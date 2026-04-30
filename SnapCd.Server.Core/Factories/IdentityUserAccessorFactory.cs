using Microsoft.AspNetCore.Identity;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Dashboard;

namespace SnapCd.Server.Core.Factories;

public class IdentityUserAccessorFactory
{
    private readonly UserManagerFactory<User, IdentityRole<Guid>, SnapCdDbContext> _userManagerFactory;
    private readonly IdentityRedirectManager _redirectManager;

    public IdentityUserAccessorFactory(
        UserManagerFactory<User, IdentityRole<Guid>, SnapCdDbContext> userManagerFactory,
        IdentityRedirectManager redirectManager)
    {
        _userManagerFactory = userManagerFactory;
        _redirectManager = redirectManager;
    }

    public IdentityUserAccessorScope Create()
    {
        var userManagerScope = _userManagerFactory.Create();
        var identityUserAccessor = new IdentityUserAccessor(userManagerScope.UserManager, _redirectManager);
        return new IdentityUserAccessorScope(identityUserAccessor, userManagerScope);
    }
}

public class IdentityUserAccessorScope : IDisposable
{
    public IdentityUserAccessor UserAccessor { get; }
    private readonly UserManagerScope<User> _userManagerScope;

    internal IdentityUserAccessorScope(IdentityUserAccessor userAccessor, UserManagerScope<User> userManagerScope)
    {
        UserAccessor = userAccessor;
        _userManagerScope = userManagerScope;
    }

    public void Dispose()
    {
        _userManagerScope?.Dispose();
    }
}