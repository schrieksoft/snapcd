using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos.Users;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Factories;
using SnapCd.Server.Core.Repositories.System.Nonsecured;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.System.Secured;

public class UserSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<UserRepositorySettings> options,
    UserManagerFactory<User, IdentityRole<Guid>, SnapCdDbContext> userManagerFactory)
{
    public UserSystemSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        var repository = new UserSystemRepository(dbContext, principalProvider, bus, options);

        var userManagerScope = userManagerFactory.Create();

        return new UserSystemSecuredRepository(repository, principalProvider, userManagerScope.UserManager);
    }
}

public class UserSystemSecuredRepository : GenericSystemSecuredRepository<User, UserReadDto, UserSystemRepository, UserCreatedEvent, UserUpdatedEvent, UserDeletedEvent, UserRepositorySettings>
{
    private readonly UserManager<User> _userManager;

    public UserSystemSecuredRepository(
        UserSystemRepository systemRepository,
        IPrincipalProvider principalProvider,
        UserManager<User> userManager
    )
        : base(systemRepository, principalProvider)
    {
        _userManager = userManager;
    }

    protected override IQueryable<User> RoleQuery<TSystemRoleAssignment>(
        Guid principalId,
        List<SystemRole> systemRoles)
    {
        return SystemRepository.DbContext.Set<User>()
            .Where(user => SystemRepository.DbContext.Set<TSystemRoleAssignment>()
                .Any(ra => ra.PrincipalId == principalId && systemRoles.Contains(ra.RoleName)));
    }

    public async Task<User> GetByUserName(string userName)
    {
        return await SystemRepository.GetByUserName(userName);
    }

    public async Task<User> GetRequiredCurrentUser()
    {
        var user = await GetCurrentUser();
        if (user == null) throw new InvalidOperationException("Current user not found");
        return user;
    }

    public async Task<User?> GetCurrentUser()
    {
        var currentUserId = PrincipalProvider.GetSystemSubjectOrDefault();
        if (currentUserId == Guid.Empty) return null;

        return await SystemRepository.Get(currentUserId);
    }

    public async Task<bool> DoesCurrentUserHavePassword()
    {
        var user = await GetCurrentUser();
        return user != null && await _userManager.HasPasswordAsync(user);
    }
}