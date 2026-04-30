using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace SnapCd.Server.Core.Factories;

public class UserManagerFactory<TUser, TRole, TDbContext>
    where TUser : IdentityUser<Guid>
    where TRole : IdentityRole<Guid>
    where TDbContext : DbContext
{
    private readonly IDbContextFactory<TDbContext> _dbContextFactory;
    private readonly IOptions<IdentityOptions> _identityOptions;
    private readonly IPasswordHasher<TUser> _passwordHasher;
    private readonly IEnumerable<IUserValidator<TUser>> _userValidators;
    private readonly IEnumerable<IPasswordValidator<TUser>> _passwordValidators;
    private readonly ILookupNormalizer _lookupNormalizer;
    private readonly IdentityErrorDescriber _errorDescriber;
    private readonly ILogger<UserManager<TUser>> _logger;
    private readonly IServiceProvider _serviceProvider;

    public UserManagerFactory(
        IDbContextFactory<TDbContext> dbContextFactory,
        IOptions<IdentityOptions> identityOptions,
        IPasswordHasher<TUser> passwordHasher,
        IEnumerable<IUserValidator<TUser>> userValidators,
        IEnumerable<IPasswordValidator<TUser>> passwordValidators,
        ILookupNormalizer lookupNormalizer,
        IdentityErrorDescriber errorDescriber,
        ILogger<UserManager<TUser>> logger,
        IServiceProvider serviceProvider
    )
    {
        _dbContextFactory = dbContextFactory;
        _identityOptions = identityOptions;
        _passwordHasher = passwordHasher;
        _userValidators = userValidators;
        _passwordValidators = passwordValidators;
        _lookupNormalizer = lookupNormalizer;
        _errorDescriber = errorDescriber;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public UserManagerScope<TUser> Create()
    {
        // Create the DbContext instance
        var context = _dbContextFactory.CreateDbContext();

        // Create the UserStore with the DbContext
        var userStore = new UserStore<TUser, TRole, TDbContext, Guid>(context);

        // Instantiate the UserManager
        var userManager = new UserManager<TUser>(
            userStore,
            _identityOptions,
            _passwordHasher,
            _userValidators,
            _passwordValidators,
            _lookupNormalizer,
            _errorDescriber,
            _serviceProvider,
            _logger
        );

        return new UserManagerScope<TUser>(userManager, context);
    }
}

public class UserManagerScope<TUser> : IDisposable
    where TUser : IdentityUser<Guid>
{
    public UserManager<TUser> UserManager { get; }
    private readonly DbContext _dbContext;

    internal UserManagerScope(UserManager<TUser> userManager, DbContext dbContext)
    {
        UserManager = userManager;
        _dbContext = dbContext;
    }

    public void Dispose()
    {
        UserManager?.Dispose();
        _dbContext?.Dispose();
    }
}