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
/// a Module probes once per wave, an edge exists once per consumer and upstream pair, the signal
/// hierarchy round-trips through one table, and the contribution status persists as text.
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
        await db.SignalContributions
            .Where(c => db.ProbeWaveParticipants
                .Where(p => _waves.Contains(p.ProbeWaveId))
                .Select(p => p.Id)
                .Contains(c.ProbeWaveParticipantId))
            .ExecuteDeleteAsync();
        await db.ProbeWaveParticipants.Where(p => _waves.Contains(p.ProbeWaveId)).ExecuteDeleteAsync();
        await db.SignalOutputs.Where(o => _signalSets.Contains(o.SignalOutputSetId)).ExecuteDeleteAsync();
        await db.SignalOutputSets.Where(s => _signalSets.Contains(s.Id)).ExecuteDeleteAsync();
        await db.ProbeWaves.Where(w => _waves.Contains(w.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task A_Module_Participates_In_A_Wave_Once()
    {
        var waveId = await SeedWave();
        await SeedParticipant(waveId, _consumerModuleId);

        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => SeedParticipant(waveId, _consumerModuleId));
    }

    [Fact]
    public async Task The_Same_Module_Participates_In_Two_Waves()
    {
        var first = await SeedWave();
        var second = await SeedWave();

        await SeedParticipant(first, _consumerModuleId);
        await SeedParticipant(second, _consumerModuleId);
    }

    [Fact]
    public async Task An_Edge_Exists_Once_Per_Consumer_And_Upstream()
    {
        var waveId = await SeedWave();
        var upstream = await SeedParticipant(waveId, _originModuleId);
        var consumer = await SeedParticipant(waveId, _consumerModuleId);

        await SeedContribution(consumer, upstream);

        await Assert.ThrowsAnyAsync<DbUpdateException>(
            () => SeedContribution(consumer, upstream));
    }

    /// <summary>
    /// The diamond's shape: one consumer waiting on two upstreams is two rows, which is what makes
    /// "both have resolved" expressible at all.
    /// </summary>
    [Fact]
    public async Task One_Consumer_Takes_A_Contribution_Per_Upstream()
    {
        var waveId = await SeedWave();
        var first = await SeedParticipant(waveId, _originModuleId);
        var second = await SeedParticipant(waveId, _fixture.Modules["0002"].Id);
        var consumer = await SeedParticipant(waveId, _consumerModuleId);

        await SeedContribution(consumer, first);
        await SeedContribution(consumer, second);

        await using var db = _fixture.CreateDbContext();
        var contributions = await db.SignalContributions.AsNoTracking()
            .Where(c => c.ProbeWaveParticipantId == consumer)
            .ToListAsync();

        Assert.Equal(2, contributions.Count);
        Assert.All(contributions, c => Assert.Equal(SignalContributionStatus.Potential, c.Status));
    }

    [Fact]
    public async Task A_Contribution_Status_Persists_As_Text()
    {
        var waveId = await SeedWave();
        var upstream = await SeedParticipant(waveId, _originModuleId);
        var consumer = await SeedParticipant(waveId, _consumerModuleId);
        var contributionId = await SeedContribution(consumer, upstream, SignalContributionStatus.NotNeeded);

        await using var db = _fixture.CreateDbContext();
        var stored = await db.Database
            .SqlQuery<string>($"SELECT [Status] AS [Value] FROM SignalContributions WHERE Id = {contributionId}")
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
            OriginModuleJobId = Guid.NewGuid(),
            MaxDepth = 5
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedParticipant(Guid waveId, Guid moduleId)
    {
        var id = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.ProbeWaveParticipants.Add(new ProbeWaveParticipant
        {
            Id = id,
            OrganizationId = _organizationId,
            ProbeWaveId = waveId,
            ModuleId = moduleId
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedContribution(
        Guid consumerId,
        Guid upstreamId,
        SignalContributionStatus status = SignalContributionStatus.Potential)
    {
        var id = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.SignalContributions.Add(new SignalContribution
        {
            Id = id,
            OrganizationId = _organizationId,
            ProbeWaveParticipantId = consumerId,
            UpstreamParticipantId = upstreamId,
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
