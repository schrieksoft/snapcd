// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services.RippleProbes;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.ProbeWaves;

/// <summary>
/// What a wave looks like the moment it is created. The contribution statuses written here decide
/// whether the first hop ever releases, so a wave created wrong stalls rather than fails.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ProbeWaveStarterTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _organizationId;
    private Guid _originModuleId;
    private Guid _consumerModuleId;

    private readonly List<Guid> _inputs = [];

    public ProbeWaveStarterTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _organizationId = _fixture.Organizations["0"].Id;
        _originModuleId = _fixture.Modules["0001"].Id;
        _consumerModuleId = _fixture.Modules["0000"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();

        var waveIds = await db.ProbeWaves
            .Where(w => w.OriginModuleId == _originModuleId || w.OriginModuleId == _consumerModuleId)
            .Select(w => w.Id)
            .ToListAsync();

        await db.SignalContributions
            .Where(c => db.ProbeWaveParticipants
                .Where(p => waveIds.Contains(p.ProbeWaveId))
                .Select(p => p.Id)
                .Contains(c.ProbeWaveParticipantId))
            .ExecuteDeleteAsync();
        await db.ProbeWaveParticipants.Where(p => waveIds.Contains(p.ProbeWaveId)).ExecuteDeleteAsync();
        var setIds = await db.SignalOutputSets.Where(x => waveIds.Contains(x.ProbeWaveId)).Select(x => x.Id).ToListAsync();
        await db.SignalOutputs.Where(o => setIds.Contains(o.SignalOutputSetId)).ExecuteDeleteAsync();
        await db.SignalOutputSets.Where(x => setIds.Contains(x.Id)).ExecuteDeleteAsync();
        await db.ProbeWaves.Where(w => waveIds.Contains(w.Id)).ExecuteDeleteAsync();

        if (_inputs.Count > 0)
            await db.ModuleInputs.Where(i => _inputs.Contains(i.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task Nothing_Downstream_Means_No_Wave()
    {
        var waveId = await Start(["vpc_id"]);

        Assert.Null(waveId);
    }

    [Fact]
    public async Task No_Changed_Outputs_Means_No_Wave()
    {
        await AddParamFromOutput(_consumerModuleId, "vpc_id", "network_id");

        Assert.Null(await Start([]));
    }

    /// <summary>
    /// The origin's own edges are written Confirmed, not Potential. The origin answers from its own
    /// plan and is never probed, so a Potential edge out of it would never resolve and the first
    /// hop would never release.
    /// </summary>
    [Fact]
    public async Task The_Origins_Edges_Are_Confirmed_At_Creation()
    {
        await AddParamFromOutput(_consumerModuleId, "vpc_id", "network_id");

        var waveId = await Start(["vpc_id"]);

        Assert.NotNull(waveId);

        await using var db = _fixture.CreateDbContext();
        var contributions = await db.SignalContributions
            .AsNoTracking()
            .Where(c => db.ProbeWaveParticipants
                .Where(p => p.ProbeWaveId == waveId!.Value)
                .Select(p => p.Id)
                .Contains(c.ProbeWaveParticipantId))
            .ToListAsync();

        Assert.NotEmpty(contributions);
        Assert.All(contributions, c => Assert.Equal(SignalContributionStatus.Confirmed, c.Status));
    }

    [Fact]
    public async Task The_Origin_Is_A_Participant()
    {
        await AddParamFromOutput(_consumerModuleId, "vpc_id", "network_id");

        var waveId = await Start(["vpc_id"]);

        await using var db = _fixture.CreateDbContext();
        var moduleIds = await db.ProbeWaveParticipants
            .AsNoTracking()
            .Where(p => p.ProbeWaveId == waveId!.Value)
            .Select(p => p.ModuleId)
            .ToListAsync();

        Assert.Contains(_originModuleId, moduleIds);
        Assert.Contains(_consumerModuleId, moduleIds);
    }

    /// <summary>
    /// Participants are the Modules downstream of the origin, not the ones it reads from. The
    /// closure holds both directions, and taking the wrong one builds a wave whose participants
    /// can never receive a signal.
    /// </summary>
    [Fact]
    public async Task A_Module_The_Origin_Reads_From_Is_Not_A_Participant()
    {
        await AddParamFromOutput(_consumerModuleId, "vpc_id", "network_id");

        // The origin reads from a third Module, which puts that Module upstream of the origin and
        // so outside the wave.
        var upstreamOfOrigin = _fixture.Modules["0010"].Id;
        await AddParamFromOutput(_originModuleId, "db_host", "database_host", outputModuleId: upstreamOfOrigin);

        var waveId = await Start(["vpc_id"]);

        await using var db = _fixture.CreateDbContext();
        var moduleIds = await db.ProbeWaveParticipants
            .AsNoTracking()
            .Where(p => p.ProbeWaveId == waveId!.Value)
            .Select(p => p.ModuleId)
            .ToListAsync();

        Assert.Contains(_consumerModuleId, moduleIds);
        Assert.DoesNotContain(upstreamOfOrigin, moduleIds);
    }

    /// <summary>
    /// A probe's Apply is an ordinary Apply, so its plan reports changed outputs. Starting a wave
    /// for it would nest a wave per probe, each counting itself as depth zero, without bound.
    /// </summary>
    [Fact]
    public async Task A_Job_That_Is_Itself_A_Probe_Starts_No_Wave()
    {
        await AddParamFromOutput(_consumerModuleId, "vpc_id", "network_id");

        var probeJobId = Guid.NewGuid();
        await AddParticipantClaiming(probeJobId);

        Assert.Null(await Start(["vpc_id"], probeJobId));
    }

    /// <summary>
    /// The origin answers from its own plan rather than by being probed, so the plan's signals are
    /// stored when the wave is created. Without them a consumer's inputs document comes out empty,
    /// because building one requires every confirmed upstream to have reported.
    /// </summary>
    [Fact]
    public async Task The_Origins_Own_Plan_Signals_Are_Recorded()
    {
        await AddParamFromOutput(_consumerModuleId, "vpc_id", "network_id");

        var signals = """
            {
              "format_version": "1",
              "outputs": {
                "vpc_id": {
                  "action": "unknown",
                  "type": "InN0cmluZyI=",
                  "before": "rHRydXN0aW5nLWxhYg==",
                  "after": "1AAA"
                }
              }
            }
            """;

        var waveId = await Start(["vpc_id"], outputSignalsJson: signals);

        await using var db = _fixture.CreateDbContext();
        var origin = await db.ProbeWaveParticipants
            .AsNoTracking()
            .FirstAsync(p => p.ProbeWaveId == waveId!.Value && p.ModuleId == _originModuleId);

        Assert.NotNull(origin.SignalOutputSetId);

        var stored = await db.SignalOutputs
            .AsNoTracking()
            .Where(o => o.SignalOutputSetId == origin.SignalOutputSetId!.Value)
            .Select(o => o.Name)
            .ToListAsync();

        Assert.Equal(["vpc_id"], stored);
    }

    [Fact]
    public async Task The_Wave_Records_Where_It_Came_From()
    {
        await AddParamFromOutput(_consumerModuleId, "vpc_id", "network_id");

        var jobId = Guid.NewGuid();
        var waveId = await Start(["vpc_id"], jobId);

        await using var db = _fixture.CreateDbContext();
        var wave = await db.ProbeWaves.AsNoTracking().FirstAsync(w => w.Id == waveId!.Value);

        Assert.Equal(_originModuleId, wave.OriginModuleId);
        Assert.Equal(jobId, wave.OriginModuleJobId);
        Assert.Equal(5, wave.MaxDepth);
    }

    /// <summary>A participant of some other wave, already claiming this job as its probe.</summary>
    private async Task AddParticipantClaiming(Guid probeJobId)
    {
        var wave = new ProbeWave
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            OriginModuleId = _consumerModuleId,
            OriginModuleJobId = Guid.NewGuid(),
            MaxDepth = 5
        };

        await using var db = _fixture.CreateDbContext();
        db.ProbeWaves.Add(wave);
        db.ProbeWaveParticipants.Add(new ProbeWaveParticipant
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ProbeWaveId = wave.Id,
            ModuleId = _originModuleId,
            LastProbeModuleJobId = probeJobId
        });
        await db.SaveChangesAsync();
    }

    private async Task<Guid?> Start(
        string[] changedOutputNames,
        Guid? originJobId = null,
        string? outputSignalsJson = null)
    {
        var factory = new FixtureDbContextFactory(_fixture);

        using var starter = new ProbeWaveStarter(
            _fixture.CreateDbContext(),
            new RippleProbeConsumerLookup(_fixture.CreateDbContext()),
            new ProbeSignalRecorderFactory(
                factory,
                Mock.Of<ISignalSecretWriter>(),
                Mock.Of<IPublishEndpoint>(),
                NullLoggerFactory.Instance),
            Mock.Of<IPublishEndpoint>(),
            new ProbePropagationSettings(),
            NullLogger<ProbeWaveStarter>.Instance);

        return await starter.Start(
            _originModuleId,
            _organizationId,
            originJobId ?? Guid.NewGuid(),
            changedOutputNames,
            outputSignalsJson);
    }

    private sealed class FixtureDbContextFactory(Fixture fixture) : IDbContextFactory<SnapCdDbContext>
    {
        public SnapCdDbContext CreateDbContext() => fixture.CreateDbContext();
    }

    private async Task AddParamFromOutput(
        Guid moduleId,
        string outputName,
        string variableName,
        Guid? outputModuleId = null)
    {
        var input = new ModuleParamFromOutput
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            OrganizationId = _organizationId,
            Name = variableName,
            OutputName = outputName,
            OutputModuleId = outputModuleId ?? _originModuleId
        };

        await using var db = _fixture.CreateDbContext();
        db.ModuleInputs.Add(input);
        await db.SaveChangesAsync();

        _inputs.Add(input.Id);
    }
}
