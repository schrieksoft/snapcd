// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics;
using Microsoft.Data.SqlClient;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Startup;

/// <summary>
/// Waits for the database server to accept connections before the Server touches it.
/// </summary>
public static class DatabaseAvailability
{
    /// <summary>
    /// Opening the configured database answers with one of these once the server itself is up: the
    /// database is absent, is still coming online, or the login cannot reach it. Migration reports
    /// each of them better than a connection probe can, so none of them is worth waiting through.
    /// </summary>
    private static readonly HashSet<int> ServerIsUp =
    [
        4060,  // Cannot open database requested by the login
        916,   // Server principal cannot access the database
        18456, // Login failed
        18470, // Login failed, account disabled
        40613, // Database currently unavailable (Azure SQL)
    ];

    public static async Task WaitAsync(
        string connectionString,
        WaitForDatabaseStartupSettings settings,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!settings.Enabled)
        {
            logger.LogDebug("Waiting for the database at startup is disabled.");
            return;
        }

        var target = new SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = settings.ConnectTimeoutSeconds
        }.ConnectionString;

        var elapsed = Stopwatch.StartNew();
        var announced = false;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await using var connection = new SqlConnection(target);
                await connection.OpenAsync(cancellationToken);
                if (announced) logger.LogInformation("Database server is accepting connections.");
                return;
            }
            catch (SqlException ex) when (ServerIsUp.Overlaps(ex.Errors.Cast<SqlError>().Select(e => e.Number)))
            {
                logger.LogDebug(ex, "Database server is up and answered {Number}; leaving the rest to migration.", ex.Number);
                return;
            }
            catch (Exception ex)
            {
                if (elapsed.Elapsed >= settings.WaitTimeout)
                {
                    logger.LogError(ex, "Database server did not accept connections within {Timeout}.", settings.WaitTimeout);
                    throw;
                }

                if (!announced)
                {
                    logger.LogInformation(
                        "Waiting up to {Timeout} for the database server to accept connections.", settings.WaitTimeout);
                    announced = true;
                }

                logger.LogDebug(ex, "Database server not reachable yet; retrying in {Interval}.", settings.RetryInterval);
                await Task.Delay(settings.RetryInterval, cancellationToken);
            }
        }
    }
}
