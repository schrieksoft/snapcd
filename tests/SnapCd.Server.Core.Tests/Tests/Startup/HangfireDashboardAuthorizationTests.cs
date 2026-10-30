// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Database;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.System;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services.Interfaces.PrincipalProvider;
using SnapCd.Server.Core.Services.Nonsecured.Admin;
using SnapCd.Server.Core.Startup;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Startup;

/// <summary>
/// Who may see the background jobs dashboard. It exposes every job's history and can trigger one
/// by hand, so the answer is system administrators and nobody else.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class HangfireDashboardAuthorizationTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private readonly List<Guid> _users = [];

    public HangfireDashboardAuthorizationTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_users.Count == 0) return;

        await using var db = _fixture.CreateDbContext();
        await db.Set<UserSystemRoleAssignment>().Where(r => _users.Contains(r.UserId)).ExecuteDeleteAsync();
        await db.Users.Where(u => _users.Contains(u.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task An_Administrator_Is_Allowed()
    {
        var administrator = await AddUser(administrator: true);

        Assert.True(await HangfireDashboardAuthorization.IsAllowed(SignedInAs(administrator)));
    }

    [Fact]
    public async Task A_User_Without_The_Role_Is_Refused()
    {
        var plain = await AddUser(administrator: false);

        Assert.False(await HangfireDashboardAuthorization.IsAllowed(SignedInAs(plain)));
    }

    /// <summary>
    /// The dashboard is mounted outside the Blazor app, so an unauthenticated request reaches the
    /// filter rather than being turned away earlier.
    /// </summary>
    [Fact]
    public async Task An_Anonymous_Request_Is_Refused()
    {
        Assert.False(await HangfireDashboardAuthorization.IsAllowed(Anonymous()));
    }

    [Fact]
    public async Task A_Request_With_No_Context_Is_Refused()
    {
        Assert.False(await HangfireDashboardAuthorization.IsAllowed(null));
    }

    private async Task<Guid> AddUser(bool administrator)
    {
        var id = Guid.NewGuid();
        _users.Add(id);

        await using var db = _fixture.CreateDbContext();
        db.Users.Add(new User
        {
            Id = id,
            UserName = $"dashboard-{id:N}@test.com",
            Email = $"dashboard-{id:N}@test.com",
            IsDisabled = false,
            CreatedDateTime = DateTime.UtcNow
        });

        if (administrator)
        {
            db.Set<UserSystemRoleAssignment>().Add(new UserSystemRoleAssignment
            {
                Id = Guid.NewGuid(),
                UserId = id,
                PrincipalId = id,
                RoleName = SystemRole.Administrator
            });
        }

        await db.SaveChangesAsync();
        return id;
    }

    private HttpContext SignedInAs(Guid userId) =>
        Context(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test")),
            new TestPrincipalProvider(userId, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id));

    private HttpContext Anonymous() =>
        Context(new ClaimsPrincipal(new ClaimsIdentity()),
            new TestPrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, _fixture.Organizations["0"].Id));

    private HttpContext Context(ClaimsPrincipal user, IPrincipalProvider principalProvider)
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        services.AddSingleton(principalProvider);
        services.AddScoped<SystemRoleServiceFactory>();

        return new DefaultHttpContext
        {
            User = user,
            RequestServices = services.BuildServiceProvider()
        };
    }
}
