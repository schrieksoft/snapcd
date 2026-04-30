using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos.Users;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.System.Nonsecured;

public class UserRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<UserRepositorySettings> options)
{
    public UserSystemRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new UserSystemRepository(dbContext, principalProvider, bus, options);
    }
}

public class UserSystemRepository : GenericSystemRepository<User, UserReadDto, UserCreatedEvent, UserUpdatedEvent, UserDeletedEvent, UserRepositorySettings>
{
    public UserSystemRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<UserRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override Func<IQueryable<User>, IQueryable<User>> ByParentIdQueryModifier(Guid parentId)
    {
        // Users have no parent, so return a query that filters nothing
        return q => q;
    }

    protected override UserReadDto MapToDto(User entity)
    {
        return UserMapper.ToDto(entity);
    }

    public async Task<User> GetByUserName(string userName)
    {
        var user = await DbContext.Users
            .SingleOrDefaultAsync<User>(i => i.UserName == userName);

        if (user == null)
            throw new EntityNotFoundException($"User with UserName {userName} not found.");

        return user;
    }
}