// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Data.SqlClient;
using SnapCd.Server.Core.Services.Resilience;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Resilience;

/// <summary>
/// Retry against the real fault it exists for: the dependency-edge race from work item 49,
/// reintroduced here by deploying the reconcile without its UPDLOCK/HOLDLOCK hint.
/// </summary>
[Collection("DependencyGraphConcurrency")]
public class TransientFaultRetryTests : IAsyncLifetime
{
    private const int Writers = 8;
    private const int Rounds = 10;
    private const int MaxAttempts = 3;

    private readonly DependencyGraphConcurrencyFixture _fixture;
    private readonly TransientFaultClassifier _classifier = new();

    public TransientFaultRetryTests(DependencyGraphConcurrencyFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync() => await DeployReconcileAsync(raceProof: false);

    public async Task DisposeAsync() => await DeployReconcileAsync(raceProof: true);

    [Fact]
    public async Task RacingWriters_SucceedThroughRetry_WithoutDuplicating()
    {
        var retries = 0;
        var failures = new List<string>();
        var gate = new object();

        for (var round = 1; round <= Rounds; round++)
        {
            await ClearInputsForPairAsync();
            using var barrier = new SemaphoreSlim(0, Writers);
            var thisRound = round;

            var writers = Enumerable.Range(0, Writers).Select(async writer =>
            {
                await barrier.WaitAsync();
                for (var attempt = 1; ; attempt++)
                {
                    try
                    {
                        await InsertInputAsync($"in_{thisRound}_{writer}");
                        return;
                    }
                    catch (Exception e) when (attempt < MaxAttempts && _classifier.IsRetryable(e, out _))
                    {
                        lock (gate) retries++;
                        await Task.Delay(20 * attempt);
                    }
                    catch (Exception e)
                    {
                        lock (gate) failures.Add($"round {thisRound} writer {writer}: {e.Message}");
                        return;
                    }
                }
            }).ToArray();

            barrier.Release(Writers);
            await Task.WhenAll(writers);
        }

        // The race must actually have fired, or this proves nothing about retry.
        Assert.True(retries > 0, "No retry occurred, so the reconcile race was not exercised.");

        Assert.True(failures.Count == 0,
            $"{failures.Count} writes failed despite retry:{Environment.NewLine}"
            + string.Join(Environment.NewLine, failures.Take(5)));

        // Every writer's row lands exactly once, and the pair still collapses to one edge.
        Assert.Equal(Writers, await ScalarAsync(
            "SELECT COUNT(*) FROM ModuleInputs WHERE ModuleId = @consumer AND OutputModuleId = @producer;"));
        Assert.Equal(1, await ScalarAsync(
            "SELECT COUNT(*) FROM DependencyEdges WHERE DefinedModuleId = @consumer AND ReferencedModuleId = @producer;"));
    }

    [Fact]
    public async Task GenuineDuplicate_IsNotRetried()
    {
        await ClearInputsForPairAsync();
        await InsertInputAsync("duplicate_probe");

        var attempts = 0;
        await Assert.ThrowsAsync<SqlException>(async () =>
        {
            for (var attempt = 1; ; attempt++)
            {
                attempts++;
                try
                {
                    await InsertInputAsync("duplicate_probe");
                    return;
                }
                catch (Exception e) when (attempt < MaxAttempts && _classifier.IsRetryable(e, out _))
                {
                    await Task.Delay(1);
                }
            }
        });

        Assert.Equal(1, attempts);
    }

    /// <summary>Deploys sp_UpdateDependencyEdgesForModules with or without the race-proofing hint.</summary>
    private async Task DeployReconcileAsync(bool raceProof)
    {
        var source = await ReadReconcileSourceAsync();
        var sql = raceProof ? source : source.Replace(" WITH (UPDLOCK, HOLDLOCK)", "");

        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> ReadReconcileSourceAsync()
    {
        var assembly = typeof(SnapCd.Server.Core.Services.ViewManagement.IdempotentSqlManager).Assembly;
        await using var stream = assembly.GetManifestResourceStream(
            "SnapCd.Server.Core.Views.SqlServer.01_DependencyGraph.sql")!;
        using var reader = new StreamReader(stream);
        var script = await reader.ReadToEndAsync();

        // Same batch split IdempotentSqlManager uses, so the extracted procedure is a valid batch.
        var batches = System.Text.RegularExpressions.Regex.Split(
            script, @"^\s*GO\s*$",
            System.Text.RegularExpressions.RegexOptions.Multiline
            | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return batches.Single(b => b.Contains("PROCEDURE sp_UpdateDependencyEdgesForModules"));
    }

    private async Task InsertInputAsync(string name) => await ExecuteAsync(
        """
        INSERT INTO ModuleInputs (Id, OrganizationId, ModuleId, Name, InputKind, Discriminator,
            OutputModuleId, OutputName, CreatedBy, CreatedByPrincipalDiscriminator, CreatedDateTime,
            ModifiedBy, ModifiedByPrincipalDiscriminator, ModifiedDateTime)
        VALUES (NEWID(), @org, @consumer, @name, 'Param', 'ModuleParamFromOutput',
            @producer, 'out', @org, 'User', SYSUTCDATETIME(), @org, 'User', SYSUTCDATETIME());
        """, name);

    private async Task ClearInputsForPairAsync() => await ExecuteAsync(
        "DELETE FROM ModuleInputs WHERE ModuleId = @consumer AND OutputModuleId = @producer;");

    private async Task ExecuteAsync(string sql, string? name = null)
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        AddParameters(command, name);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<int> ScalarAsync(string sql)
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        AddParameters(command, null);
        return (int)(await command.ExecuteScalarAsync())!;
    }

    private void AddParameters(SqlCommand command, string? name)
    {
        command.Parameters.AddWithValue("@org", _fixture.OrganizationId);
        command.Parameters.AddWithValue("@consumer", _fixture.ConsumerModuleId);
        command.Parameters.AddWithValue("@producer", _fixture.ProducerModuleId);
        if (name is not null)
            command.Parameters.AddWithValue("@name", name);
    }
}
