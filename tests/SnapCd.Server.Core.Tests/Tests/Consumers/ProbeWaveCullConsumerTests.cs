// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SnapCd.Server.Core.Consumers.System.Competing;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Entities.Definition.Signals;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Factories.Vaults;
using SnapCd.Server.Core.Services.Interfaces.Vaults;
using SnapCd.Server.Core.Services.Nonsecured.Vaults;
using SnapCd.Server.Core.Services.Utils.Vaults;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Consumers;

/// <summary>
/// The wave is the unit a cull counts, so what matters is that one delete takes the whole episode:
/// the attempts, the edges and both kinds of set, with the sensitive signals' values reclaimed from
/// the vault, which the SQL cascade cannot do on its own.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ProbeWaveCullConsumerTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _organizationId;
    private Guid _originModuleId;
    private readonly List<Guid> _waves = [];
    private readonly List<Guid> _jobs = [];

    public ProbeWaveCullConsumerTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _organizationId = _fixture.Organizations["0"].Id;
        _originModuleId = _fixture.Modules["0000"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        await db.ProbeWaves.Where(w => _waves.Contains(w.Id)).ExecuteDeleteAsync();
        await db.ModuleJobs.Where(j => _jobs.Contains(j.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task Culling_off_keeps_every_wave()
    {
        var waves = await SeedWaves(4);

        await Consume(Retention(false, 2));

        Assert.Equal(4, await CountWaves(waves));
    }

    [Fact]
    public async Task Culling_on_keeps_the_newest_up_to_the_limit()
    {
        var waves = await SeedWaves(4);

        await Consume(Retention(true, 2));

        Assert.Equal(2, await CountWaves(waves));

        await using var db = _fixture.CreateDbContext();
        var survivors = await db.ProbeWaves
            .Where(w => waves.Contains(w.Id))
            .OrderByDescending(w => w.CreatedDateTime)
            .Select(w => w.Id)
            .ToListAsync();

        Assert.Equal([waves[3], waves[2]], survivors);
    }

    [Fact]
    public async Task An_origin_under_the_limit_loses_nothing()
    {
        var waves = await SeedWaves(2);

        await Consume(Retention(true, 5));

        Assert.Equal(2, await CountWaves(waves));
    }

    [Fact]
    public async Task A_limit_below_one_culls_nothing()
    {
        var waves = await SeedWaves(3);

        await Consume(Retention(true, 0));

        Assert.Equal(3, await CountWaves(waves));
    }

    /// <summary>
    /// One delete takes the episode. The attempt is what makes this worth asserting: it points at
    /// both sets under a restriction, so a cull that did not take it in the same cascade would be
    /// refused by the database.
    /// </summary>
    [Fact]
    public async Task Culling_a_wave_takes_its_attempts_edges_and_both_kinds_of_set()
    {
        var waves = await SeedWaves(2);
        var doomed = waves[0];

        var signalSetId = await SeedSignalSet(doomed, _originModuleId);
        var changeSetId = await SeedResourceChangeSet(doomed);
        var attemptId = await SeedAttempt(doomed, signalSetId, changeSetId);
        var edgeId = await SeedEdge(doomed);

        await Consume(Retention(true, 1));

        await using var db = _fixture.CreateDbContext();
        Assert.False(await db.ProbeWaves.AnyAsync(w => w.Id == doomed));
        Assert.False(await db.ModuleProbeAttempts.AnyAsync(a => a.Id == attemptId));
        Assert.False(await db.SignalEdges.AnyAsync(e => e.Id == edgeId));
        Assert.False(await db.SignalOutputSets.AnyAsync(s => s.Id == signalSetId));
        Assert.False(await db.ResourceChangeSets.AnyAsync(s => s.Id == changeSetId));
    }

    /// <summary>
    /// The rows go by SQL cascade, which raises no event, so without the sweep the values would be
    /// left in the vault with nothing left in the database naming them.
    /// </summary>
    [Fact]
    public async Task Culling_a_wave_reclaims_its_sensitive_signal_values()
    {
        var waves = await SeedWaves(2);
        var doomed = waves[0];

        var setId = await SeedSignalSet(doomed, _originModuleId);
        var remoteName = RemoteSecretName.ForSignal(_organizationId, _originModuleId, setId);

        var vault = NewVault();
        await vault.SetSecretAsync(remoteName, """{"after":"secret"}""");

        await SeedSecretSignal(setId, remoteName);

        Assert.NotNull(await vault.GetSecretAsync(remoteName));

        await Consume(Retention(true, 1));

        // The vault reports a missing secret by throwing rather than returning null.
        await Assert.ThrowsAsync<KeyNotFoundException>(() => NewVault().GetSecretAsync(remoteName));
    }

    /// <summary>
    /// A wave of a different origin is not this origin's to count, so a cull triggered by one
    /// Module must not reach another's history.
    /// </summary>
    [Fact]
    public async Task A_cull_counts_only_the_origin_that_triggered_it()
    {
        var mine = await SeedWaves(3);
        var theirs = await SeedWaves(2, _fixture.Modules["0001"].Id);

        await Consume(Retention(true, 1));

        Assert.Equal(1, await CountWaves(mine));
        Assert.Equal(2, await CountWaves(theirs));
    }

    private static SetRetentionSettings Retention(bool cull, int max) => new()
    {
        ProbeWaves = new ProbeWaveRetentionSettings
        {
            CullAboveLimit = cull,
            MaxKeptPerModule = max
        }
    };

    private async Task Consume(SetRetentionSettings settings)
    {
        await using var consumerDb = _fixture.CreateDbContext();
        await using var sweeperDb = _fixture.CreateDbContext();

        var sweeper = new OrphanedVaultValueSweeper(
            sweeperDb,
            new FixtureVaultFactory(_fixture),
            Options.Create(new SecretStoreSettings()),
            NullLogger<OrphanedVaultValueSweeper>.Instance);

        var context = new Mock<ConsumeContext<ProbeWaveCreatedEvent>>();
        context.Setup(c => c.Message).Returns(new ProbeWaveCreatedEvent
        {
            ProbeWaveId = Guid.NewGuid(),
            OrganizationId = _organizationId,
            OriginModuleId = _originModuleId
        });

        await new ProbeWaveCullConsumer(
            consumerDb,
            sweeper,
            Options.Create(settings),
            NullLogger<ProbeWaveCullConsumer>.Instance).Consume(context.Object);
    }

    private async Task<int> CountWaves(List<Guid> ids)
    {
        await using var db = _fixture.CreateDbContext();
        return await db.ProbeWaves.CountAsync(w => ids.Contains(w.Id));
    }

    /// <summary>Waves for one origin, oldest first in the returned list.</summary>
    private async Task<List<Guid>> SeedWaves(int count, Guid? originModuleId = null)
    {
        var moduleId = originModuleId ?? _originModuleId;
        var ids = new List<Guid>();

        for (var i = 0; i < count; i++)
        {
            var jobId = await AddOriginJob(moduleId);
            var id = Guid.NewGuid();

            await using var db = _fixture.CreateDbContext();
            db.ProbeWaves.Add(new ProbeWave
            {
                Id = id,
                OrganizationId = _organizationId,
                OriginModuleJobId = jobId,
                CreatedDateTime = DateTime.UtcNow.AddMinutes(i)
            });
            await db.SaveChangesAsync();

            _waves.Add(id);
            ids.Add(id);
        }

        return ids;
    }

    private async Task<Guid> AddOriginJob(Guid moduleId)
    {
        var jobId = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(new ModuleJob
        {
            Id = jobId,
            OrganizationId = _organizationId,
            ModuleId = moduleId,
            JobType = "ApplyJobSaga",
            Status = ExecutionStatus.Running,
            WaitingForApproval = true,
            TimestampStart = DateTimeOffset.UtcNow.AddTicks(_jobs.Count),
            CreatedDateTime = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        _jobs.Add(jobId);
        return jobId;
    }

    private async Task<Guid> SeedSignalSet(Guid waveId, Guid moduleId)
    {
        var id = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.SignalOutputSets.Add(new SignalOutputSet
        {
            Id = id,
            OrganizationId = _organizationId,
            ModuleId = moduleId,
            ProbeWaveId = waveId,
            ModuleJobId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Checksum = Guid.NewGuid().ToString()
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedResourceChangeSet(Guid waveId)
    {
        var id = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.ResourceChangeSets.Add(new ResourceChangeSet
        {
            Id = id,
            OrganizationId = _organizationId,
            ModuleId = _originModuleId,
            ProbeWaveId = waveId,
            ModuleJobId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            TotalCount = 1
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedAttempt(Guid waveId, Guid signalSetId, Guid changeSetId)
    {
        var id = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.ModuleProbeAttempts.Add(new ModuleProbeAttempt
        {
            Id = id,
            OrganizationId = _organizationId,
            ProbeWaveId = waveId,
            ModuleId = _originModuleId,
            SignalOutputSetId = signalSetId,
            ResourceChangeSetId = changeSetId
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedEdge(Guid waveId)
    {
        var id = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.SignalEdges.Add(new SignalEdge
        {
            Id = id,
            OrganizationId = _organizationId,
            ProbeWaveId = waveId,
            ConsumerModuleId = _fixture.Modules["0001"].Id,
            UpstreamModuleId = _originModuleId,
            Status = SignalEdgeStatus.Confirmed
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task SeedSecretSignal(Guid setId, string remoteName)
    {
        await using var db = _fixture.CreateDbContext();
        db.SecretSignalOutputs.Add(new SecretSignalOutput
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            SignalOutputSetId = setId,
            Name = "db_password",
            Sensitive = true,
            RemoteSecretName = remoteName
        });
        await db.SaveChangesAsync();
    }

    private SqlVault NewVault() =>
        new(new FixtureDbContextFactory(_fixture), new byte[32], NullLogger<SqlVault>.Instance);

    private sealed class FixtureDbContextFactory(Fixture fixture) : IDbContextFactory<SnapCdDbContext>
    {
        public SnapCdDbContext CreateDbContext() => fixture.CreateDbContext();
    }

    private sealed class FixtureVaultFactory(Fixture fixture) : IVaultFactory
    {
        public IVault Create(string vaultUrl) =>
            new SqlVault(new FixtureDbContextFactory(fixture), new byte[32], NullLogger<SqlVault>.Instance);
    }
}
