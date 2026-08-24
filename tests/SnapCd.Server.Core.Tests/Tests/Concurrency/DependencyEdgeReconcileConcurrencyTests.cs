// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Data.SqlClient;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Concurrency;

/// <summary>
/// Terraform creates a module's inputs in parallel, so several transactions reconcile the same
/// DependencyEdges row at once. The reconcile must tolerate that.
/// </summary>
[Collection("DependencyGraphConcurrency")]
public class DependencyEdgeReconcileConcurrencyTests
{
    private const int Workers = 8;
    private const int Rounds = 25;

    private const int UniqueViolation = 2627;
    private const int UniqueIndexViolation = 2601;
    private const int Deadlock = 1205;

    private readonly DependencyGraphConcurrencyFixture _fixture;

    public DependencyEdgeReconcileConcurrencyTests(DependencyGraphConcurrencyFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConcurrentInputsForOneModulePair_DoNotCollideOnTheEdgeRow()
    {
        var failures = new List<string>();
        var failureLock = new object();

        for (var round = 1; round <= Rounds; round++)
        {
            // Clearing the pair drops the edge, so every round forces the reconcile's insert arm.
            // Without this the edge survives the first round and the race window never reopens.
            await ClearInputsForPairAsync();

            // The writers are released together: the defect only appears when they pass the
            // existence check inside each other's window.
            using var gate = new SemaphoreSlim(0, Workers);
            var thisRound = round;

            var writers = Enumerable.Range(0, Workers).Select(async worker =>
            {
                await using var connection = new SqlConnection(_fixture.ConnectionString);
                await connection.OpenAsync();
                await gate.WaitAsync();
                try
                {
                    // Distinct names — nothing here is a genuine duplicate.
                    await InsertInputAsync($"in_{thisRound}_{worker}", connection);
                }
                catch (SqlException e) when (e.Number is UniqueViolation or UniqueIndexViolation or Deadlock)
                {
                    lock (failureLock)
                        failures.Add($"round {thisRound} worker {worker}: {e.Number} {e.Message}");
                }
            }).ToArray();

            gate.Release(Workers);
            await Task.WhenAll(writers);
        }

        Assert.True(failures.Count == 0,
            $"{failures.Count} of {Workers * Rounds} concurrent inserts failed:{Environment.NewLine}" +
            string.Join(Environment.NewLine, failures.Take(10)));

        Assert.Equal(1, await CountEdgeRowsForPairAsync());
    }

    [Fact]
    public async Task GenuineDuplicateInput_StillViolatesTheUniqueIndex()
    {
        await ClearInputsForPairAsync();
        await InsertInputAsync("duplicate_probe");

        var e = await Assert.ThrowsAsync<SqlException>(() => InsertInputAsync("duplicate_probe"));

        Assert.Contains(e.Number, new[] { UniqueViolation, UniqueIndexViolation });
        Assert.Contains("ModuleInputs", e.Message);
    }

    private async Task InsertInputAsync(string name, SqlConnection? existing = null)
    {
        const string sql = """
            INSERT INTO ModuleInputs (Id, OrganizationId, ModuleId, Name, InputKind, Discriminator,
                OutputModuleId, OutputName, CreatedBy, CreatedByPrincipalDiscriminator, CreatedDateTime,
                ModifiedBy, ModifiedByPrincipalDiscriminator, ModifiedDateTime)
            VALUES (NEWID(), @org, @consumer, @name, 'Param', 'ModuleParamFromOutput',
                @producer, 'out', @org, 'User', SYSUTCDATETIME(), @org, 'User', SYSUTCDATETIME());
            """;

        await using var owned = existing is null ? new SqlConnection(_fixture.ConnectionString) : null;
        var connection = existing ?? owned!;
        if (existing is null)
            await connection.OpenAsync();

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@org", _fixture.OrganizationId);
        command.Parameters.AddWithValue("@consumer", _fixture.ConsumerModuleId);
        command.Parameters.AddWithValue("@producer", _fixture.ProducerModuleId);
        command.Parameters.AddWithValue("@name", name);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ClearInputsForPairAsync()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "DELETE FROM ModuleInputs WHERE ModuleId = @consumer AND OutputModuleId = @producer;", connection);
        command.Parameters.AddWithValue("@consumer", _fixture.ConsumerModuleId);
        command.Parameters.AddWithValue("@producer", _fixture.ProducerModuleId);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<int> CountEdgeRowsForPairAsync()
    {
        await using var connection = new SqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(
            "SELECT COUNT(*) FROM DependencyEdges WHERE DefinedModuleId = @consumer AND ReferencedModuleId = @producer;",
            connection);
        command.Parameters.AddWithValue("@consumer", _fixture.ConsumerModuleId);
        command.Parameters.AddWithValue("@producer", _fixture.ProducerModuleId);
        return (int)(await command.ExecuteScalarAsync())!;
    }
}
