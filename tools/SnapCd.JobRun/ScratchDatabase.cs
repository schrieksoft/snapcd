// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Services.CallerContext;
using SnapCd.Server.Core.Services.DataSeeder;
using SnapCd.Server.Core.Services.ViewManagement;

namespace SnapCd.JobRun;

/// <summary>
/// A database of the run's own, built from the migrations and thrown away afterwards. Nothing a
/// server or another run holds is touched, so a run that leaves state behind costs nothing.
/// </summary>
public static class ScratchDatabase
{
    /// <summary>
    /// The transport's own migration runs as a hosted service, so the database has to exist
    /// before the host starts.
    /// </summary>
    public static async Task Create(RunOptions options)
    {
        await using var connection = new SqlConnection(MasterConnection(options));
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{options.DatabaseName}]";
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Migrations, then the idempotent scripts and the seed, as the server does at startup.</summary>
    public static async Task Prepare(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();

        var db = scope.ServiceProvider.GetRequiredService<SnapCdDbContext>();
        await db.Database.MigrateAsync();

        await scope.ServiceProvider.GetRequiredService<IIdempotentSqlManager>()
            .ApplyIdempotentSqlAsync();

        using (CallerContext.Begin(CallerKind.System))
            await scope.ServiceProvider.GetRequiredService<IDataSeeder>().SeedAsync();
    }

    public static async Task Drop(RunOptions options)
    {
        SqlConnection.ClearAllPools();

        await using var connection = new SqlConnection(MasterConnection(options));
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"ALTER DATABASE [{options.DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; "
            + $"DROP DATABASE [{options.DatabaseName}]";
        await command.ExecuteNonQueryAsync();
    }

    private static string MasterConnection(RunOptions options) =>
        $"{options.MasterConnectionString.TrimEnd(';')};Database=master";
}
