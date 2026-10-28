// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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