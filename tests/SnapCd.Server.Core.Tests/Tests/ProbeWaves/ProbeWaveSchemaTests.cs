// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Entities.Definition.Signals;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.ProbeWaves;

/// <summary>
/// What the schema promises, asserted against the database rather than read off the class maps:
/// a Module can probe more than once in a wave, an edge exists once per consumer and upstream
/// pair, the signal hierarchy round-trips through one table, and the status persists as text.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ProbeWaveSchemaTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _organizationId;
    private Guid _originModuleId;
    private Guid _consumerModuleId;
    private readonly List<Guid> _waves = [];
    private readonly List<Guid> _signalSets = [];

    public ProbeWaveSchemaTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _organizationId = _fixture.Organizations["0"].Id;
        _originModuleId = _fixture.Modules["0000"].Id;
        _consumerModuleId = _fixture.Modules["0001"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        await db.SignalEdges.Where(e => _waves.Contains(e.ProbeWaveId)).ExecuteDeleteAsync();
        await db.ModuleProbeAttempts.Where(p => _waves.Contains(p.ProbeWaveId)).ExecuteDeleteAsync();
        await db.SignalOutputs.Where(o => _signalSets.Contains(o.SignalOutputSetId)).ExecuteDeleteAsync();
        await db.SignalOutputSets.Where(s => _signalSets.Contains(s.Id)).ExecuteDeleteAsync();
        await db.ProbeWaves.Where(w => _waves.Contains(w.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task A_Module_Probes_In_A_Wave_More_Than_Once()
    {
        var waveId = await SeedWave();

        // A probe is repeatable: a Module that failed is probed again by a later Apply, and each
        // attempt keeps its own row.
        await SeedAttempt(waveId, _consumerModuleId);
        await SeedAttempt(waveId, _consumerModuleId);

        await using var db = _fixture.CreateDbContext();
        var attempts = await db.ModuleProbeAttempts
            .CountAsync(a => a.ProbeWaveId == waveId && a.ModuleId == _consumerModuleId);

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task The_Same_Module_Probes_In_Two_Waves()
    {
        var first = await SeedWave();
        var second = await SeedWave();

        await SeedAttempt(first, _consumerModuleId);
        await SeedAttempt(second, _consumerModuleId);
    }

    [Fact]
    public async Task An_Edge_Exists_Once_Per_Consumer_And_Upstream()
    {
        var waveId = await SeedWave();

        await SeedEdge(waveId, _consumerModuleId, _originModuleId);

        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => SeedEdge(waveId, _consumerModuleId, _originModuleId));
    }

    /// <summary>
    /// The diamond's shape: one consumer waiting on two upstreams is two rows, which is what makes
    /// "both have resolved" expressible at all.
    /// </summary>
    [Fact]
    public async Task One_Consumer_Takes_An_Edge_Per_Upstream()
    {
        var waveId = await SeedWave();

        await SeedEdge(waveId, _consumerModuleId, _originModuleId);
        await SeedEdge(waveId, _consumerModuleId, _fixture.Modules["0002"].Id);

        await using var db = _fixture.CreateDbContext();
        var edges = await db.SignalEdges.AsNoTracking()
            .Where(e => e.ProbeWaveId == waveId && e.ConsumerModuleId == _consumerModuleId)
            .ToListAsync();

        Assert.Equal(2, edges.Count);
        Assert.All(edges, e => Assert.Equal(SignalEdgeStatus.Potential, e.Status));
    }

    [Fact]
    public async Task An_Edge_Status_Persists_As_Text()
    {
        var waveId = await SeedWave();
        var edgeId = await SeedEdge(
            waveId, _consumerModuleId, _originModuleId, SignalEdgeStatus.NotNeeded);

        await using var db = _fixture.CreateDbContext();
        var stored = await db.Database
            .SqlQuery<string>($"SELECT [Status] AS [Value] FROM SignalEdges WHERE Id = {edgeId}")
            .SingleAsync();

        Assert.Equal("NotNeeded", stored);
    }

    /// <summary>
    /// A sensitive signal and a plain one share a table, so the discriminator is what keeps them
    /// apart. Reading each back through its own set proves the mapping rather than the class map.
    /// </summary>
    [Fact]
    public async Task Both_Kinds_Of_Signal_Round_Trip_Through_One_Table()
    {
        var waveId = await SeedWave();
        var setId = await SeedSignalSet(waveId);

        const string signalJson =
            """{"action":"unknown","type":"InN0cmluZyI=","before":"sGNoYW1waW9uLWhlcnJpbmc=","after":"1AAA"}""";

        await using (var db = _fixture.CreateDbContext())
        {
            db.LiteralSignalOutputs.Add(new LiteralSignalOutput
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                SignalOutputSetId = setId,
                Name = "pet_id",
                Sensitive = false,
                SignalJson = signalJson
            });
            db.SecretSignalOutputs.Add(new SecretSignalOutput
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                SignalOutputSetId = setId,
                Name = "db_password",
                Sensitive = true,
                RemoteSecretName = "signal-db-password"
            });
            await db.SaveChangesAsync();
        }

        await using var check = _fixture.CreateDbContext();

        var literal = await check.LiteralSignalOutputs.AsNoTracking()
            .SingleAsync(o => o.SignalOutputSetId == setId);
        Assert.Equal(signalJson, literal.SignalJson);
        Assert.False(literal.Sensitive);

        var secret = await check.SecretSignalOutputs.AsNoTracking()
            .SingleAsync(o => o.SignalOutputSetId == setId);
        Assert.Equal("signal-db-password", secret.RemoteSecretName);
        Assert.True(secret.Sensitive);

        // The base set sees both, which is what one table means.
        Assert.Equal(2, await check.SignalOutputs.AsNoTracking().CountAsync(o => o.SignalOutputSetId == setId));
    }

    [Fact]
    public async Task An_Output_Name_Appears_Once_Per_Signal_Set()
    {
        var waveId = await SeedWave();
        var setId = await SeedSignalSet(waveId);

        await SeedSignal(setId, "pet_id");

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => SeedSignal(setId, "pet_id"));
    }

    private async Task<Guid> SeedWave()
    {
        var id = Guid.NewGuid();
        _waves.Add(id);

        await using var db = _fixture.CreateDbContext();
        db.ProbeWaves.Add(new ProbeWave
        {
            Id = id,
            OrganizationId = _organizationId,
            OriginModuleId = _originModuleId,
            OriginModuleJobId = Guid.NewGuid()
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedAttempt(Guid waveId, Guid moduleId)
    {
        var id = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.ModuleProbeAttempts.Add(new ModuleProbeAttempt
        {
            Id = id,
            OrganizationId = _organizationId,
            ProbeWaveId = waveId,
            ModuleId = moduleId
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedEdge(
        Guid waveId,
        Guid consumerModuleId,
        Guid upstreamModuleId,
        SignalEdgeStatus status = SignalEdgeStatus.Potential)
    {
        var id = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.SignalEdges.Add(new SignalEdge
        {
            Id = id,
            OrganizationId = _organizationId,
            ProbeWaveId = waveId,
            ConsumerModuleId = consumerModuleId,
            UpstreamModuleId = upstreamModuleId,
            Status = status
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedSignalSet(Guid waveId)
    {
        var id = Guid.NewGuid();
        _signalSets.Add(id);

        await using var db = _fixture.CreateDbContext();
        db.SignalOutputSets.Add(new SignalOutputSet
        {
            Id = id,
            OrganizationId = _organizationId,
            ModuleId = _originModuleId,
            ProbeWaveId = waveId,
            ModuleJobId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Checksum = Guid.NewGuid().ToString()
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task SeedSignal(Guid setId, string name)
    {
        await using var db = _fixture.CreateDbContext();
        db.LiteralSignalOutputs.Add(new LiteralSignalOutput
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            SignalOutputSetId = setId,
            Name = name,
            Sensitive = false,
            SignalJson = "{}"
        });
        await db.SaveChangesAsync();
    }
}
