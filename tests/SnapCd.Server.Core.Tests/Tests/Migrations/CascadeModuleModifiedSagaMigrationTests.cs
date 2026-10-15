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
using SnapCd.Server.Host.Database;
using Testcontainers.MsSql;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Migrations;

/// <summary>
/// One-off, for the CascadeModuleModifiedSaga migration. The shared fixture migrates a fresh
/// database, where nothing is orphaned, so it cannot exercise what this migration is for: a
/// database that already carries debouncer rows whose Modules are gone. Those rows violate the
/// foreign key the migration adds, so without the sweep the migration cannot apply at all.
///
/// Its own container, because it has to stop at the migration before and insert rows by hand.
/// Delete this class once the migration has been deployed everywhere it needs to be.
/// </summary>
public class CascadeModuleModifiedSagaMigrationTests : IAsyncLifetime
{
    private const string Password = "TestPass123!";
    private const string DatabaseName = "SnapCdMigrationTest";
    private const string Previous = "20261008140000_DependencyEdgeLockOrder";
    private const string Target = "20261015162320_CascadeModuleModifiedSaga";

    private MsSqlContainer _container = null!;
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword(Password)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();

        await _container.StartAsync();

        var master = _container.GetConnectionString();

        // The wait strategy only checks the port, which SQL Server opens before it accepts a
        // login, so the first attempts fail with "Login failed for user 'sa'". Fixture retries for
        // the same reason.
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

        // Password explicitly: GetConnectionString does not carry it onto a rebuilt string, which
        // is why Fixture sets it the same way.
        _connectionString = new SqlConnectionStringBuilder(master)
        {
            InitialCatalog = DatabaseName,
            Password = Password
        }.ConnectionString;
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task The_Migration_Sweeps_Orphans_And_Then_Cascades()
    {
        // Up to the migration before this one, so the old direction is still in place.
        await Migrate(Previous);

        await AssertForeignKey(
            expectedTable: "Modules",
            expectedAction: "NO_ACTION",
            because: "the old mapping had Modules carrying the reference under Restrict");

        var orphanId = await InsertOrphanedSaga();
        Assert.Equal(1, await CountSagas(orphanId));

        // The migration under test.
        await Migrate(Target);

        Assert.Equal(0, await CountSagas(orphanId));

        await AssertForeignKey(
            expectedTable: "ModuleModifiedSaga",
            expectedAction: "CASCADE",
            because: "the saga is the dependent now, and goes with its Module");
    }

    private async Task Migrate(string target)
    {
        var services = new ServiceCollection();
        services.AddDbContext<SelfHostedSnapCdDbContext>(options =>
        {
            options.UseSqlServer(_connectionString,
                sql => sql.MigrationsAssembly("SnapCd.Server.Host"));
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SelfHostedSnapCdDbContext>();
        await db.Database.GetService<IMigrator>().MigrateAsync(target);
    }

    /// <summary>
    /// Written with raw SQL rather than the entity, because the point is a row the model would not
    /// let exist: a debouncer whose Module is not there.
    /// </summary>
    private async Task<Guid> InsertOrphanedSaga()
    {
        var correlationId = Guid.NewGuid();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var insert = new SqlCommand(
            """
            INSERT INTO dbo.ModuleModifiedSaga (CorrelationId, OrganizationId, CurrentState)
            VALUES (@id, @org, 'Idle');
            """, connection);
        insert.Parameters.AddWithValue("@id", correlationId);
        insert.Parameters.AddWithValue("@org", Guid.NewGuid());
        await insert.ExecuteNonQueryAsync();

        return correlationId;
    }

    private async Task<int> CountSagas(Guid correlationId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var count = new SqlCommand(
            "SELECT COUNT(*) FROM dbo.ModuleModifiedSaga WHERE CorrelationId = @id;", connection);
        count.Parameters.AddWithValue("@id", correlationId);
        return (int)(await count.ExecuteScalarAsync())!;
    }

    private async Task AssertForeignKey(string expectedTable, string expectedAction, string because)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var query = new SqlCommand(
            """
            SELECT OBJECT_NAME(fk.parent_object_id), fk.delete_referential_action_desc
            FROM sys.foreign_keys fk
            WHERE fk.name LIKE '%ModuleModifiedSaga%';
            """, connection);

        await using var reader = await query.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync(), "no foreign key between Modules and ModuleModifiedSaga");

        Assert.Equal(expectedTable, reader.GetString(0));
        Assert.Equal(expectedAction, reader.GetString(1));
        Assert.False(await reader.ReadAsync(), $"expected exactly one, {because}");
    }
}
