// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using DotNet.Testcontainers.Builders;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Host.Database;
using Testcontainers.MsSql;

namespace SnapCd.Server.Core.Tests.Tests.Migrations;

/// <summary>
/// The StacksBecomeRootNamespaces migration against a database that already holds Stacks, which the
/// shared fixture's freshly migrated one does not. Its own container, because it has to stop at the
/// migration before and add rows.
/// </summary>
public class StacksBecomeRootNamespacesMigrationTests : IAsyncLifetime
{
    private const string Password = "TestPass123!";
    private const string DatabaseName = "SnapCdMigrationTest";
    private const string Previous = "20261123173556_InputKindFromDiscriminator";
    private const string Target = "20261128174321_StacksBecomeRootNamespaces";
    private const string ScopeFromType = "20261128190019_SecretScopeKindFromDiscriminator";

    private MsSqlContainer _container = null!;
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword(Password)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(1433))
            .Build();

        await _container.StartAsync();

        var master = _container.GetConnectionString();

        // The port opens before SQL Server accepts a login.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var connection = new SqlConnection(master);
                await connection.OpenAsync();
                await using var create = new SqlCommand($"CREATE DATABASE [{DatabaseName}];", connection);
                await create.ExecuteNonQueryAsync();
                break;
            }
            catch (SqlException) when (attempt < 30)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        _connectionString = new SqlConnectionStringBuilder(master)
        {
            InitialCatalog = DatabaseName,
            Password = Password
        }.ConnectionString;
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task A_stack_holding_a_namespace_of_its_own_name_migrates_and_rolls_back()
    {
        await Migrate(Previous);

        var org = Guid.NewGuid();
        var stack = Guid.NewGuid();
        var sameName = Guid.NewGuid();
        var other = Guid.NewGuid();
        var secret = Guid.NewGuid();

        await using (var db = Context())
        {
            db.Organizations.Add(new Organization { Id = org, Name = "org", Status = OrganizationStatus.Active, CreatedDateTime = DateTime.UtcNow });
            db.Stacks.Add(new Stack { Id = stack, OrganizationId = org, Name = "prod", CreatedDateTime = DateTime.UtcNow });
            db.Namespaces.Add(new Namespace { Id = sameName, OrganizationId = org, StackId = stack, Name = "prod", CreatedDateTime = DateTime.UtcNow });
            db.Namespaces.Add(new Namespace { Id = other, OrganizationId = org, StackId = stack, Name = "network", CreatedDateTime = DateTime.UtcNow });
            await db.SaveChangesAsync();

            // By hand: at this migration the scope is still a column the application writes.
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO dbo.Secrets (Id, OrganizationId, StackId, Name, Discriminator, ScopeKind, NameContainsScope, " +
                "CreatedBy, CreatedByPrincipalDiscriminator, CreatedDateTime, ModifiedBy, ModifiedByPrincipalDiscriminator, ModifiedDateTime) " +
                "VALUES ({0}, {1}, {2}, N'token', N'StackSecret', N'Stack', 1, {3}, N'User', SYSUTCDATETIME(), {3}, N'User', SYSUTCDATETIME())",
                secret, org, stack, Guid.Empty);
        }

        await Migrate(Target);

        await using (var db = Context())
        {
            var root = await db.Namespaces.AsNoTracking().SingleAsync(n => n.Id == stack);
            Assert.Equal("prod", root.Name);
            Assert.Null(root.ParentNamespaceId);

            Assert.Equal(stack, (await db.Namespaces.AsNoTracking().SingleAsync(n => n.Id == sameName)).ParentNamespaceId);
            Assert.Equal(stack, (await db.Namespaces.AsNoTracking().SingleAsync(n => n.Id == other)).ParentNamespaceId);
            Assert.Equal(stack, (await db.NamespaceSecrets.AsNoTracking().SingleAsync(s => s.Id == secret)).NamespaceId);
            Assert.Equal(1, await db.NamespaceAncestors.CountAsync(a => a.NamespaceId == other && a.AncestorNamespaceId == stack && a.Depth == 1));

            // Two Namespaces with one name under one parent are still refused.
            db.Namespaces.Add(new Namespace
            {
                Id = Guid.NewGuid(), OrganizationId = org, StackId = stack, ParentNamespaceId = stack, Name = "network", CreatedDateTime = DateTime.UtcNow
            });
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        await Migrate(ScopeFromType);
        Assert.Equal("Namespace", await Scope(secret));

        await using (var db = Context())
        {
            // Rolling back refuses a name that only the new index allows, so the Namespace named after its Stack goes first.
            await db.Namespaces.Where(n => n.Id == sameName).ExecuteDeleteAsync();
        }

        await Migrate(Previous);

        await using (var db = Context())
        {
            Assert.False(await db.Namespaces.AnyAsync(n => n.Id == stack));
            Assert.Null((await db.Namespaces.AsNoTracking().SingleAsync(n => n.Id == other)).ParentNamespaceId);
            Assert.Equal(stack, (await db.Secrets.OfType<StackSecret>().AsNoTracking().SingleAsync(s => s.Id == secret)).StackId);
        }

        Assert.Equal("Stack", await Scope(secret));
    }

    private async Task<string> Scope(Guid secretId)
    {
        await using var db = Context();
        return await db.Database
            .SqlQueryRaw<string>("SELECT ScopeKind AS Value FROM dbo.Secrets WHERE Id = {0}", secretId)
            .SingleAsync();
    }

    private SelfHostedSnapCdDbContext Context()
    {
        var options = new DbContextOptionsBuilder<SelfHostedSnapCdDbContext>()
            .UseSqlServer(_connectionString, sql => sql.MigrationsAssembly("SnapCd.Server.Host"))
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new SelfHostedSnapCdDbContext(options);
    }

    private async Task Migrate(string target)
    {
        await using var db = Context();
        await db.Database.GetService<IMigrator>().MigrateAsync(target);
    }
}
