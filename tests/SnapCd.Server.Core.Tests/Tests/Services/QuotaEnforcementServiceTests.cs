using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.ViewManagement;
using SnapCd.Server.Core.Settings;
using Testcontainers.MsSql;

namespace SnapCd.Server.Core.Tests.Tests.Services;

public class QuotaEnforcementServiceTests : IAsyncLifetime
{
    private IContainer? _databaseContainer;
    private ServiceProvider? _serviceProvider;
    private IDbContextFactory<SnapCdDbContext>? _dbContextFactory;

    // Pre-defined organization IDs for each test scenario
    private static readonly Guid Org_NoQuotaConfigured = Guid.Parse("00000000-0000-0000-0002-000000000001");
    private static readonly Guid Org_UnderQuota = Guid.Parse("00000000-0000-0000-0002-000000000002");
    private static readonly Guid Org_OverQuotaUnprotected = Guid.Parse("00000000-0000-0000-0002-000000000003");
    private static readonly Guid Org_OverQuotaDirectOwner = Guid.Parse("00000000-0000-0000-0002-000000000004");
    private static readonly Guid Org_OverQuotaDirectIAM = Guid.Parse("00000000-0000-0000-0002-000000000005");
    private static readonly Guid Org_OverQuotaGroupProtected = Guid.Parse("00000000-0000-0000-0002-000000000006");
    private static readonly Guid Org_OverQuotaNestedGroup = Guid.Parse("00000000-0000-0000-0002-000000000007");
    private static readonly Guid Org_AllProtected = Guid.Parse("00000000-0000-0000-0002-000000000008");
    private static readonly Guid Org_MixedProtection = Guid.Parse("00000000-0000-0000-0002-000000000009");


    public async Task InitializeAsync()
    {
        // Start SQL Server container
        _databaseContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("TestPass123!")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();

        await _databaseContainer.StartAsync();
        var connectionString = ((MsSqlContainer)_databaseContainer).GetConnectionString();

        // Configure services
        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });
        services.AddDbContext<SnapCdDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddScoped<IViewManager, ViewManager>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>();

        // Run migrations and apply views
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SnapCdDbContext>();
        await dbContext.Database.MigrateAsync();

        var viewManager = scope.ServiceProvider.GetRequiredService<IViewManager>();
        await viewManager.ApplyViewsAsync();

        // Seed test data
        await SeedTestData(dbContext);
    }

    private async Task SeedTestData(SnapCdDbContext dbContext)
    {
        // Create all organizations
        var orgIds = new[]
        {
            Org_NoQuotaConfigured, Org_UnderQuota, Org_OverQuotaUnprotected,
            Org_OverQuotaDirectOwner, Org_OverQuotaDirectIAM, Org_OverQuotaGroupProtected,
            Org_OverQuotaNestedGroup, Org_AllProtected, Org_MixedProtection
        };

        foreach (var orgId in orgIds)
        {
            dbContext.Organizations.Add(new Organization
            {
                Id = orgId,
                Name = $"TestOrg-{orgId}",
                CreatedBy = Guid.Empty,
                CreatedDateTime = DateTime.UtcNow,
                ModifiedBy = Guid.Empty,
                ModifiedDateTime = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();

        // Seed users for each scenario
        await SeedNoQuotaConfiguredScenario(dbContext);
        await SeedUnderQuotaScenario(dbContext);
        await SeedOverQuotaUnprotectedScenario(dbContext);
        await SeedOverQuotaDirectOwnerScenario(dbContext);
        await SeedOverQuotaDirectIAMScenario(dbContext);
        await SeedOverQuotaGroupProtectedScenario(dbContext);
        await SeedOverQuotaNestedGroupScenario(dbContext);
        await SeedAllProtectedScenario(dbContext);
        await SeedMixedProtectionScenario(dbContext);

        await dbContext.SaveChangesAsync();
    }

    private async Task SeedNoQuotaConfiguredScenario(SnapCdDbContext dbContext)
    {
        // 5 users, but no quota configured for this org
        for (var i = 0; i < 5; i++)
        {
            var userId = Guid.NewGuid();
            dbContext.Users.Add(CreateUser(userId, $"noquota{i}@test.com"));
            dbContext.OrganizationUsers.Add(CreateOrgUser(Org_NoQuotaConfigured, userId, DateTime.UtcNow.AddDays(-i)));
        }

        await Task.CompletedTask;
    }

    private async Task SeedUnderQuotaScenario(SnapCdDbContext dbContext)
    {
        // 2 users, quota will be 3 - under quota
        for (var i = 0; i < 2; i++)
        {
            var userId = Guid.NewGuid();
            dbContext.Users.Add(CreateUser(userId, $"underquota{i}@test.com"));
            dbContext.OrganizationUsers.Add(CreateOrgUser(Org_UnderQuota, userId, DateTime.UtcNow.AddDays(-i)));
        }

        await Task.CompletedTask;
    }

    private async Task SeedOverQuotaUnprotectedScenario(SnapCdDbContext dbContext)
    {
        // 5 users, quota will be 3 - 2 should be deactivated (newest first)
        // Oldest user (day -4), then middle users, then newest (day 0)
        for (var i = 0; i < 5; i++)
        {
            var userId = Guid.NewGuid();
            dbContext.Users.Add(CreateUser(userId, $"overunprotected{i}@test.com"));
            dbContext.OrganizationUsers.Add(CreateOrgUser(Org_OverQuotaUnprotected, userId, DateTime.UtcNow.AddDays(-i)));
        }

        await Task.CompletedTask;
    }

    private async Task SeedOverQuotaDirectOwnerScenario(SnapCdDbContext dbContext)
    {
        // 4 users, quota will be 2
        // One has direct Owner role - should be protected
        // Deactivate 2 newest unprotected users
        var ownerUserId = Guid.NewGuid();
        dbContext.Users.Add(CreateUser(ownerUserId, "directowner@test.com"));
        var ownerOrgUser = CreateOrgUser(Org_OverQuotaDirectOwner, ownerUserId, DateTime.UtcNow.AddDays(-3));
        dbContext.OrganizationUsers.Add(ownerOrgUser);

        // Add Owner role to this user
        dbContext.UserOrganizationRoleAssignments.Add(new UserOrganizationRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = Org_OverQuotaDirectOwner,
            UserId = ownerUserId,
            RoleName = OrganizationRole.Owner,
            PrincipalDiscriminator = RoleAssignmentPrincipalDiscriminator.User,
            CreatedBy = Guid.Empty,
            CreatedDateTime = DateTime.UtcNow,
            ModifiedBy = Guid.Empty,
            ModifiedDateTime = DateTime.UtcNow
        });

        // Add 3 more unprotected users
        for (var i = 0; i < 3; i++)
        {
            var userId = Guid.NewGuid();
            dbContext.Users.Add(CreateUser(userId, $"directowner_unprotected{i}@test.com"));
            dbContext.OrganizationUsers.Add(CreateOrgUser(Org_OverQuotaDirectOwner, userId, DateTime.UtcNow.AddDays(-i)));
        }

        await Task.CompletedTask;
    }

    private async Task SeedOverQuotaDirectIAMScenario(SnapCdDbContext dbContext)
    {
        // 4 users, quota will be 2
        // One has direct IdentityAccessManager role - should be protected
        var iamUserId = Guid.NewGuid();
        dbContext.Users.Add(CreateUser(iamUserId, "directiam@test.com"));
        dbContext.OrganizationUsers.Add(CreateOrgUser(Org_OverQuotaDirectIAM, iamUserId, DateTime.UtcNow.AddDays(-3)));

        // Add IAM role to this user
        dbContext.UserOrganizationRoleAssignments.Add(new UserOrganizationRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = Org_OverQuotaDirectIAM,
            UserId = iamUserId,
            RoleName = OrganizationRole.IdentityAccessManager,
            PrincipalDiscriminator = RoleAssignmentPrincipalDiscriminator.User,
            CreatedBy = Guid.Empty,
            CreatedDateTime = DateTime.UtcNow,
            ModifiedBy = Guid.Empty,
            ModifiedDateTime = DateTime.UtcNow
        });

        // Add 3 more unprotected users
        for (var i = 0; i < 3; i++)
        {
            var userId = Guid.NewGuid();
            dbContext.Users.Add(CreateUser(userId, $"directiam_unprotected{i}@test.com"));
            dbContext.OrganizationUsers.Add(CreateOrgUser(Org_OverQuotaDirectIAM, userId, DateTime.UtcNow.AddDays(-i)));
        }

        await Task.CompletedTask;
    }

    private async Task SeedOverQuotaGroupProtectedScenario(SnapCdDbContext dbContext)
    {
        // 4 users, quota will be 2
        // One is member of a group with Owner role - should be protected

        // Create the group
        var groupId = Guid.NewGuid();
        dbContext.Groups.Add(new Group
        {
            Id = groupId,
            OrganizationId = Org_OverQuotaGroupProtected,
            Name = "Admins",
            CreatedBy = Guid.Empty,
            CreatedDateTime = DateTime.UtcNow,
            ModifiedBy = Guid.Empty,
            ModifiedDateTime = DateTime.UtcNow
        });

        // Assign Owner role to the group
        dbContext.GroupOrganizationRoleAssignments.Add(new GroupOrganizationRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = Org_OverQuotaGroupProtected,
            GroupId = groupId,
            RoleName = OrganizationRole.Owner,
            PrincipalDiscriminator = RoleAssignmentPrincipalDiscriminator.Group,
            CreatedBy = Guid.Empty,
            CreatedDateTime = DateTime.UtcNow,
            ModifiedBy = Guid.Empty,
            ModifiedDateTime = DateTime.UtcNow
        });

        // Create user and add to group
        var protectedUserId = Guid.NewGuid();
        dbContext.Users.Add(CreateUser(protectedUserId, "groupprotected@test.com"));
        dbContext.OrganizationUsers.Add(CreateOrgUser(Org_OverQuotaGroupProtected, protectedUserId, DateTime.UtcNow.AddDays(-3)));

        dbContext.UserGroupMembers.Add(new UserGroupMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = Org_OverQuotaGroupProtected,
            GroupId = groupId,
            UserId = protectedUserId,
            GroupMemberDiscriminator = GroupMemberDiscriminator.User,
            CreatedBy = Guid.Empty,
            CreatedDateTime = DateTime.UtcNow,
            ModifiedBy = Guid.Empty,
            ModifiedDateTime = DateTime.UtcNow
        });

        // Add 3 more unprotected users
        for (var i = 0; i < 3; i++)
        {
            var userId = Guid.NewGuid();
            dbContext.Users.Add(CreateUser(userId, $"groupprotected_unprotected{i}@test.com"));
            dbContext.OrganizationUsers.Add(CreateOrgUser(Org_OverQuotaGroupProtected, userId, DateTime.UtcNow.AddDays(-i)));
        }

        await Task.CompletedTask;
    }

    private async Task SeedOverQuotaNestedGroupScenario(SnapCdDbContext dbContext)
    {
        // 4 users, quota will be 2
        // One is member of GroupA, which is member of GroupB, which has Owner role

        // Create GroupB (parent group with Owner role)
        var groupBId = Guid.NewGuid();
        dbContext.Groups.Add(new Group
        {
            Id = groupBId,
            OrganizationId = Org_OverQuotaNestedGroup,
            Name = "ParentAdmins",
            CreatedBy = Guid.Empty,
            CreatedDateTime = DateTime.UtcNow,
            ModifiedBy = Guid.Empty,
            ModifiedDateTime = DateTime.UtcNow
        });

        // Assign Owner role to GroupB
        dbContext.GroupOrganizationRoleAssignments.Add(new GroupOrganizationRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = Org_OverQuotaNestedGroup,
            GroupId = groupBId,
            RoleName = OrganizationRole.Owner,
            PrincipalDiscriminator = RoleAssignmentPrincipalDiscriminator.Group,
            CreatedBy = Guid.Empty,
            CreatedDateTime = DateTime.UtcNow,
            ModifiedBy = Guid.Empty,
            ModifiedDateTime = DateTime.UtcNow
        });

        // Create GroupA (child group)
        var groupAId = Guid.NewGuid();
        dbContext.Groups.Add(new Group
        {
            Id = groupAId,
            OrganizationId = Org_OverQuotaNestedGroup,
            Name = "ChildAdmins",
            CreatedBy = Guid.Empty,
            CreatedDateTime = DateTime.UtcNow,
            ModifiedBy = Guid.Empty,
            ModifiedDateTime = DateTime.UtcNow
        });

        // Add GroupA as member of GroupB
        dbContext.GroupGroupMembers.Add(new GroupGroupMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = Org_OverQuotaNestedGroup,
            GroupId = groupBId, // Parent group
            MemberGroupId = groupAId, // Child group
            GroupMemberDiscriminator = GroupMemberDiscriminator.Group,
            CreatedBy = Guid.Empty,
            CreatedDateTime = DateTime.UtcNow,
            ModifiedBy = Guid.Empty,
            ModifiedDateTime = DateTime.UtcNow
        });

        // Create user and add to GroupA
        var protectedUserId = Guid.NewGuid();
        dbContext.Users.Add(CreateUser(protectedUserId, "nestedprotected@test.com"));
        dbContext.OrganizationUsers.Add(CreateOrgUser(Org_OverQuotaNestedGroup, protectedUserId, DateTime.UtcNow.AddDays(-3)));

        dbContext.UserGroupMembers.Add(new UserGroupMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = Org_OverQuotaNestedGroup,
            GroupId = groupAId, // User is in child group
            UserId = protectedUserId,
            GroupMemberDiscriminator = GroupMemberDiscriminator.User,
            CreatedBy = Guid.Empty,
            CreatedDateTime = DateTime.UtcNow,
            ModifiedBy = Guid.Empty,
            ModifiedDateTime = DateTime.UtcNow
        });

        // Add 3 more unprotected users
        for (var i = 0; i < 3; i++)
        {
            var userId = Guid.NewGuid();
            dbContext.Users.Add(CreateUser(userId, $"nestedprotected_unprotected{i}@test.com"));
            dbContext.OrganizationUsers.Add(CreateOrgUser(Org_OverQuotaNestedGroup, userId, DateTime.UtcNow.AddDays(-i)));
        }

        await Task.CompletedTask;
    }

    private async Task SeedAllProtectedScenario(SnapCdDbContext dbContext)
    {
        // 4 users, all have Owner role, quota will be 2
        // Cannot deactivate any - should log warning
        for (var i = 0; i < 4; i++)
        {
            var userId = Guid.NewGuid();
            dbContext.Users.Add(CreateUser(userId, $"allprotected{i}@test.com"));
            dbContext.OrganizationUsers.Add(CreateOrgUser(Org_AllProtected, userId, DateTime.UtcNow.AddDays(-i)));

            dbContext.UserOrganizationRoleAssignments.Add(new UserOrganizationRoleAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = Org_AllProtected,
                UserId = userId,
                RoleName = OrganizationRole.Owner,
                PrincipalDiscriminator = RoleAssignmentPrincipalDiscriminator.User,
                CreatedBy = Guid.Empty,
                CreatedDateTime = DateTime.UtcNow,
                ModifiedBy = Guid.Empty,
                ModifiedDateTime = DateTime.UtcNow
            });
        }

        await Task.CompletedTask;
    }

    private async Task SeedMixedProtectionScenario(SnapCdDbContext dbContext)
    {
        // 5 users, quota will be 2
        // 2 have Owner role (protected), 3 unprotected
        // Should deactivate 3 unprotected users (newest first)

        // 2 protected users (oldest)
        for (var i = 0; i < 2; i++)
        {
            var userId = Guid.NewGuid();
            dbContext.Users.Add(CreateUser(userId, $"mixedprotected{i}@test.com"));
            dbContext.OrganizationUsers.Add(CreateOrgUser(Org_MixedProtection, userId, DateTime.UtcNow.AddDays(-10 - i)));

            dbContext.UserOrganizationRoleAssignments.Add(new UserOrganizationRoleAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = Org_MixedProtection,
                UserId = userId,
                RoleName = OrganizationRole.Owner,
                PrincipalDiscriminator = RoleAssignmentPrincipalDiscriminator.User,
                CreatedBy = Guid.Empty,
                CreatedDateTime = DateTime.UtcNow,
                ModifiedBy = Guid.Empty,
                ModifiedDateTime = DateTime.UtcNow
            });
        }

        // 3 unprotected users (newer)
        for (var i = 0; i < 3; i++)
        {
            var userId = Guid.NewGuid();
            dbContext.Users.Add(CreateUser(userId, $"mixedunprotected{i}@test.com"));
            dbContext.OrganizationUsers.Add(CreateOrgUser(Org_MixedProtection, userId, DateTime.UtcNow.AddDays(-i)));
        }

        await Task.CompletedTask;
    }

    private static User CreateUser(Guid userId, string email) => new()
    {
        Id = userId,
        Email = email,
        UserName = email,
        CreatedBy = Guid.Empty,
        CreatedDateTime = DateTime.UtcNow,
        ModifiedBy = Guid.Empty,
        ModifiedDateTime = DateTime.UtcNow
    };

    private static OrganizationUser CreateOrgUser(Guid orgId, Guid userId, DateTime createdDateTime) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = orgId,
        UserId = userId,
        JoinedAt = createdDateTime,
        IsDeactivated = false,
        CreatedBy = Guid.Empty,
        CreatedDateTime = createdDateTime,
        ModifiedBy = Guid.Empty,
        ModifiedDateTime = createdDateTime
    };

    private QuotaEnforcementService CreateService(int? userQuota)
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var mockLogger = new Mock<ILogger<LicenseService>>();
        var mockSaaSLicenseClient = new Mock<ISaaSLicenseClient>();
        var mockPublicKeyService = new Mock<ILicensePublicKeyService>();
        var debuggingOptions = Options.Create(new DebuggingOptions());
        var licenseService = new LicenseService(_dbContextFactory!, memoryCache, mockSaaSLicenseClient.Object, mockPublicKeyService.Object, debuggingOptions, mockLogger.Object);
        var quotaGatingService = new QuotaGatingService(licenseService);
        var quotaService = new QuotaService(quotaGatingService);
        var logger = NullLogger<QuotaEnforcementService>.Instance;
        return new QuotaEnforcementService(_dbContextFactory!, quotaService, logger);
    }

    public async Task DisposeAsync()
    {
        _serviceProvider?.Dispose();
        if (_databaseContainer != null)
        {
            await _databaseContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task EnforceUserQuotaAsync_NoQuotaConfigured_NoDeactivation()
    {
        // Arrange
        var service = CreateService(null); // No quota

        // Act
        await service.EnforceUserQuotaAsync(Org_NoQuotaConfigured);

        // Assert - No users should be deactivated
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync();
        var activeUsers = await dbContext.OrganizationUsers
            .Where(ou => ou.OrganizationId == Org_NoQuotaConfigured && !ou.IsDeactivated)
            .CountAsync();
        Assert.Equal(5, activeUsers);
    }

    [Fact]
    public async Task EnforceUserQuotaAsync_UnderQuota_NoDeactivation()
    {
        // Arrange
        var service = CreateService(3); // Quota of 3, only 2 users

        // Act
        await service.EnforceUserQuotaAsync(Org_UnderQuota);

        // Assert - No users should be deactivated
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync();
        var activeUsers = await dbContext.OrganizationUsers
            .Where(ou => ou.OrganizationId == Org_UnderQuota && !ou.IsDeactivated)
            .CountAsync();
        Assert.Equal(2, activeUsers);
    }

    [Fact]
    public async Task EnforceUserQuotaAsync_OverQuotaUnprotected_DeactivatesNewestFirst()
    {
        // Arrange
        var service = CreateService(3); // Quota of 3, has 5 users

        // Act
        await service.EnforceUserQuotaAsync(Org_OverQuotaUnprotected);

        // Assert - 2 newest users should be deactivated, 3 remain
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync();
        var activeUsers = await dbContext.OrganizationUsers
            .Where(ou => ou.OrganizationId == Org_OverQuotaUnprotected && !ou.IsDeactivated)
            .OrderBy(ou => ou.CreatedDateTime)
            .ToListAsync();

        Assert.Equal(3, activeUsers.Count);

        // Verify the oldest 3 are active (newest were deactivated)
        var deactivatedUsers = await dbContext.OrganizationUsers
            .Where(ou => ou.OrganizationId == Org_OverQuotaUnprotected && ou.IsDeactivated)
            .ToListAsync();
        Assert.Equal(2, deactivatedUsers.Count);
    }

    [Fact]
    public async Task EnforceUserQuotaAsync_OverQuotaWithDirectOwner_OwnerNotDeactivated()
    {
        // Arrange
        var service = CreateService(2); // Quota of 2, has 4 users (1 owner + 3 unprotected)

        // Get the owner user ID
        await using var dbContextSetup = await _dbContextFactory!.CreateDbContextAsync();
        var ownerUserId = await dbContextSetup.UserOrganizationRoleAssignments
            .Where(ra => ra.OrganizationId == Org_OverQuotaDirectOwner && ra.RoleName == OrganizationRole.Owner)
            .Select(ra => ra.UserId)
            .FirstAsync();

        // Act
        await service.EnforceUserQuotaAsync(Org_OverQuotaDirectOwner);

        // Assert - Owner should still be active
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync();
        var ownerOrgUser = await dbContext.OrganizationUsers
            .FirstAsync(ou => ou.OrganizationId == Org_OverQuotaDirectOwner && ou.UserId == ownerUserId);
        Assert.False(ownerOrgUser.IsDeactivated, "Owner should not be deactivated");

        // Should have 2 active users total (owner + 1 oldest unprotected)
        var activeCount = await dbContext.OrganizationUsers
            .CountAsync(ou => ou.OrganizationId == Org_OverQuotaDirectOwner && !ou.IsDeactivated);
        Assert.Equal(2, activeCount);
    }

    [Fact]
    public async Task EnforceUserQuotaAsync_OverQuotaWithDirectIAM_IAMNotDeactivated()
    {
        // Arrange
        var service = CreateService(2); // Quota of 2, has 4 users (1 IAM + 3 unprotected)

        // Get the IAM user ID
        await using var dbContextSetup = await _dbContextFactory!.CreateDbContextAsync();
        var iamUserId = await dbContextSetup.UserOrganizationRoleAssignments
            .Where(ra => ra.OrganizationId == Org_OverQuotaDirectIAM && ra.RoleName == OrganizationRole.IdentityAccessManager)
            .Select(ra => ra.UserId)
            .FirstAsync();

        // Act
        await service.EnforceUserQuotaAsync(Org_OverQuotaDirectIAM);

        // Assert - IAM should still be active
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync();
        var iamOrgUser = await dbContext.OrganizationUsers
            .FirstAsync(ou => ou.OrganizationId == Org_OverQuotaDirectIAM && ou.UserId == iamUserId);
        Assert.False(iamOrgUser.IsDeactivated, "IdentityAccessManager should not be deactivated");

        var activeCount = await dbContext.OrganizationUsers
            .CountAsync(ou => ou.OrganizationId == Org_OverQuotaDirectIAM && !ou.IsDeactivated);
        Assert.Equal(2, activeCount);
    }

    [Fact]
    public async Task EnforceUserQuotaAsync_OverQuotaWithGroupProtected_GroupMemberNotDeactivated()
    {
        // Arrange
        var service = CreateService(2); // Quota of 2, has 4 users (1 group-protected + 3 unprotected)

        // Get the group-protected user ID
        await using var dbContextSetup = await _dbContextFactory!.CreateDbContextAsync();
        var protectedUserId = await dbContextSetup.UserGroupMembers
            .Where(ugm => ugm.OrganizationId == Org_OverQuotaGroupProtected)
            .Select(ugm => ugm.UserId)
            .FirstAsync();

        // Act
        await service.EnforceUserQuotaAsync(Org_OverQuotaGroupProtected);

        // Assert - Group member should still be active
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync();
        var protectedOrgUser = await dbContext.OrganizationUsers
            .FirstAsync(ou => ou.OrganizationId == Org_OverQuotaGroupProtected && ou.UserId == protectedUserId);
        Assert.False(protectedOrgUser.IsDeactivated, "Group-protected user should not be deactivated");

        var activeCount = await dbContext.OrganizationUsers
            .CountAsync(ou => ou.OrganizationId == Org_OverQuotaGroupProtected && !ou.IsDeactivated);
        Assert.Equal(2, activeCount);
    }

    [Fact]
    public async Task EnforceUserQuotaAsync_OverQuotaWithNestedGroup_NestedGroupMemberNotDeactivated()
    {
        // Arrange
        var service = CreateService(2); // Quota of 2, has 4 users (1 nested-protected + 3 unprotected)

        // Get the nested-protected user ID (member of GroupA which is member of GroupB with Owner role)
        await using var dbContextSetup = await _dbContextFactory!.CreateDbContextAsync();
        var protectedUserId = await dbContextSetup.UserGroupMembers
            .Where(ugm => ugm.OrganizationId == Org_OverQuotaNestedGroup)
            .Select(ugm => ugm.UserId)
            .FirstAsync();

        // Act
        await service.EnforceUserQuotaAsync(Org_OverQuotaNestedGroup);

        // Assert - Nested group member should still be active
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync();
        var protectedOrgUser = await dbContext.OrganizationUsers
            .FirstAsync(ou => ou.OrganizationId == Org_OverQuotaNestedGroup && ou.UserId == protectedUserId);
        Assert.False(protectedOrgUser.IsDeactivated, "Nested group-protected user should not be deactivated");

        var activeCount = await dbContext.OrganizationUsers
            .CountAsync(ou => ou.OrganizationId == Org_OverQuotaNestedGroup && !ou.IsDeactivated);
        Assert.Equal(2, activeCount);
    }

    [Fact]
    public async Task EnforceUserQuotaAsync_AllUsersProtected_NoDeactivationAndLogsWarning()
    {
        // Arrange
        var service = CreateService(2); // Quota of 2, has 4 users all with Owner role

        // Act
        await service.EnforceUserQuotaAsync(Org_AllProtected);

        // Assert - All users should still be active (cannot deactivate protected users)
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync();
        var activeCount = await dbContext.OrganizationUsers
            .CountAsync(ou => ou.OrganizationId == Org_AllProtected && !ou.IsDeactivated);
        Assert.Equal(4, activeCount); // All 4 remain active despite quota of 2
    }

    [Fact]
    public async Task EnforceUserQuotaAsync_MixedProtection_OnlyUnprotectedDeactivated()
    {
        // Arrange
        var service = CreateService(2); // Quota of 2, has 5 users (2 protected, 3 unprotected)

        // Get protected user IDs
        await using var dbContextSetup = await _dbContextFactory!.CreateDbContextAsync();
        var protectedUserIds = await dbContextSetup.UserOrganizationRoleAssignments
            .Where(ra => ra.OrganizationId == Org_MixedProtection && ra.RoleName == OrganizationRole.Owner)
            .Select(ra => ra.UserId)
            .ToListAsync();

        // Act
        await service.EnforceUserQuotaAsync(Org_MixedProtection);

        // Assert
        await using var dbContext = await _dbContextFactory!.CreateDbContextAsync();

        // Both protected users should still be active
        foreach (var protectedUserId in protectedUserIds)
        {
            var protectedOrgUser = await dbContext.OrganizationUsers
                .FirstAsync(ou => ou.OrganizationId == Org_MixedProtection && ou.UserId == protectedUserId);
            Assert.False(protectedOrgUser.IsDeactivated, $"Protected user {protectedUserId} should not be deactivated");
        }

        // Should have only 2 active users (the 2 protected ones)
        // All 3 unprotected should be deactivated to get down to quota of 2
        var activeCount = await dbContext.OrganizationUsers
            .CountAsync(ou => ou.OrganizationId == Org_MixedProtection && !ou.IsDeactivated);
        Assert.Equal(2, activeCount);

        var deactivatedCount = await dbContext.OrganizationUsers
            .CountAsync(ou => ou.OrganizationId == Org_MixedProtection && ou.IsDeactivated);
        Assert.Equal(3, deactivatedCount);
    }
}