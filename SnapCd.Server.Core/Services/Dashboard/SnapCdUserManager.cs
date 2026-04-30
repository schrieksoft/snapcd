using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.Dashboard;

public class SnapCdUserManager : UserManager<User>
{
    private readonly SnapCdDbContext _dbContext;

    public SnapCdUserManager(
        IUserStore<User> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<User> passwordHasher,
        IEnumerable<IUserValidator<User>> userValidators,
        IEnumerable<IPasswordValidator<User>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<SnapCdUserManager> logger,
        SnapCdDbContext dbContext)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
        _dbContext = dbContext;
    }


    public virtual User? GetUserAsync(string userName)
    {
        var user = _dbContext.Users
            .SingleOrDefault(x => x.UserName == userName);

        return user;
    }

    public virtual bool UserExists(User user)
    {
        return Queryable
            .Any<User>(_dbContext.Users, x => x.UserName == user.UserName);
    }

    public virtual User? GetExistingUser(string userName)
    {
        return _dbContext.Users
            .SingleOrDefault(x => x.UserName == userName);
    }
}