// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.System;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services.Admin;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Services;

[Collection("NewRoleBasedSharedFixture")]
public class SystemRoleServiceTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private ServiceProvider _provider = null!;

    public SystemRoleServiceTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        _provider = services.BuildServiceProvider(true);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Administrator_Assignment_Grants_And_Its_Absence_Denies()
    {
        Guid adminUserId, plainUserId;
        await using (var db = _fixture.CreateDbContext())
        {
            var admin = new User { Id = Guid.NewGuid(), UserName = $"sysadmin-{Guid.NewGuid():N}@test.com", Email = "sysadmin@test.com", IsDisabled = false, CreatedDateTime = DateTime.UtcNow };
            var plain = new User { Id = Guid.NewGuid(), UserName = $"plain-{Guid.NewGuid():N}@test.com", Email = "plain@test.com", IsDisabled = false, CreatedDateTime = DateTime.UtcNow };
            db.Users.AddRange(admin, plain);
            db.Set<UserSystemRoleAssignment>().Add(new UserSystemRoleAssignment
            {
                Id = Guid.NewGuid(),
                UserId = admin.Id,
                PrincipalId = admin.Id,
                RoleName = SystemRole.Administrator
            });
            await db.SaveChangesAsync();
            adminUserId = admin.Id;
            plainUserId = plain.Id;
        }

        var factory = _provider.GetRequiredService<IDbContextFactory<SnapCdDbContext>>();
        var service = new SystemRoleService(factory, Mock.Of<IPrincipalProvider>());

        Assert.True(await service.UserIsSystemAdministratorAsync(adminUserId));
        Assert.False(await service.UserIsSystemAdministratorAsync(plainUserId));
        Assert.False(await service.UserIsSystemAdministratorAsync(Guid.Empty));
    }
}
