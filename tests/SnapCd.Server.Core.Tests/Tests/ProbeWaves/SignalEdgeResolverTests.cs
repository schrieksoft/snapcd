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
using Moq;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Entities.Definition.Signals;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services.Nonsecured.RippleProbes;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.ProbeWaves;

/// <summary>
/// What happens to a wave when one of its Modules reports.
///
/// This is where a wave grows: the Modules reading what was just reported join it. Growing too far
/// probes Modules the change never reached; not growing far enough stops the propagation one hop
/// short, which is the failure the whole work item exists to prevent.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class SignalEdgeResolverTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _organizationId;
    private Guid _producerModuleId;
    private Guid _consumerModuleId;
    private Guid _secondHopModuleId;

    private readonly List<Guid> _waves = [];
    private readonly List<Guid> _originJobs = [];
    private readonly List<Guid> _inputs = [];

    public SignalEdgeResolverTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _organizationId = _fixture.Organizations["0"].Id;
        _producerModuleId = _fixture.Modules["0000"].Id;
        _consumerModuleId = _fixture.Modules["0001"].Id;
        _secondHopModuleId = _fixture.Modules["0002"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();

                var setIds = await db.SignalOutputSets
            .Where(s => _waves.Contains(s.ProbeWaveId)).Select(s => s.Id).ToListAsync();

        await db.SignalEdges.Where(e => _waves.Contains(e.ProbeWaveId)).ExecuteDeleteAsync();
        await db.ModuleProbeAttempts.Where(p => _waves.Contains(p.ProbeWaveId)).ExecuteDeleteAsync();
        await db.SignalOutputs.Where(o => setIds.Contains(o.SignalOutputSetId)).ExecuteDeleteAsync();
        await db.SignalOutputSets.Where(s => setIds.Contains(s.Id)).ExecuteDeleteAsync();
        await db.ProbeWaves.Where(w => _waves.Contains(w.Id)).ExecuteDeleteAsync();
        await db.ModuleJobs.Where(j => _originJobs.Contains(j.Id)).ExecuteDeleteAsync();

        if (_inputs.Count > 0)
            await db.ModuleInputs.Where(i => _inputs.Contains(i.Id)).ExecuteDeleteAsync();
    }

    /// <summary>
    /// The hop. A Module reading the output that just changed joins the wave with an edge back to
    /// the Module that reported, which is how a chain continues past its first link.
    /// </summary>
    [Fact]
    public async Task A_Consumer_Of_A_Changed_Output_Joins_The_Wave()
    {
        await AddParamFromOutput(_secondHopModuleId, _consumerModuleId, "consumer_id", "from_consumer");

        var wave = await AddWave();
        await AddAttempt(wave, _consumerModuleId);
        await AddSignals(wave, _consumerModuleId, ("consumer_id", "unknown"));

        await Resolve(wave, _consumerModuleId);

        await using var db = _fixture.CreateDbContext();
        var edge = await db.SignalEdges
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ProbeWaveId == wave &&
                                      e.ConsumerModuleId == _secondHopModuleId);

        Assert.NotNull(edge);
        Assert.Equal(_consumerModuleId, edge!.UpstreamModuleId);
    }

    /// <summary>
    /// The control case the fixture was extended for. An output the plan considered and found
    /// unchanged is reported as a no-op, and a Module reading only that must stay out of the wave.
    /// </summary>
    [Fact]
    public async Task A_Consumer_Of_An_Unchanged_Output_Does_Not_Join()
    {
        await AddParamFromOutput(_secondHopModuleId, _consumerModuleId, "consumer_other", "from_other");

        var wave = await AddWave();
        await AddAttempt(wave, _consumerModuleId);
        await AddSignals(wave, _consumerModuleId,
            ("consumer_id", "unknown"),
            ("consumer_other", "no-op"));

        await Resolve(wave, _consumerModuleId);

        await using var db = _fixture.CreateDbContext();
        var reached = await db.SignalEdges
            .AsNoTracking()
            .AnyAsync(e => e.ProbeWaveId == wave && e.ConsumerModuleId == _secondHopModuleId);

        Assert.False(reached);
    }

    /// <summary>
    /// A Module reachable from itself takes part once. A wave is finite in Modules rather than in
    /// hops, which is what makes a cycle settle instead of growing forever.
    /// </summary>
    [Fact]
    public async Task A_Module_Already_In_The_Wave_Is_Not_Enrolled_Twice()
    {
        await AddParamFromOutput(_producerModuleId, _consumerModuleId, "consumer_id", "from_consumer");

        var wave = await AddWave();
        await AddAttempt(wave, _producerModuleId);
        await AddAttempt(wave, _consumerModuleId);
        await AddSignals(wave, _consumerModuleId, ("consumer_id", "unknown"));

        await Resolve(wave, _consumerModuleId);

        await using var db = _fixture.CreateDbContext();
        var rows = await db.SignalEdges
            .AsNoTracking()
            .CountAsync(e => e.ProbeWaveId == wave && e.ConsumerModuleId == _producerModuleId);

        Assert.Equal(1, rows);

        // The edge back to it is still written, so the cycle is recorded rather than ignored.
        var edge = await db.SignalEdges
            .AsNoTracking()
            .AnyAsync(e => e.ConsumerModuleId == _producerModuleId &&
                           e.UpstreamModuleId == _consumerModuleId);

        Assert.True(edge);
    }

    /// <summary>
    /// A Module destroyed while a wave is open answers it rather than leaving it waiting. The edge
    /// it owes points at it as the consumer and is already Confirmed, which is the row
    /// ResolveAfterSignals does not touch.
    /// </summary>
    [Fact]
    public async Task A_Destroyed_Module_Resolves_The_Edge_It_Owes()
    {
        var wave = await AddWave();

        await using (var seed = _fixture.CreateDbContext())
        {
            seed.SignalEdges.Add(new SignalEdge
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ProbeWaveId = wave,
                ConsumerModuleId = _consumerModuleId,
                UpstreamModuleId = _producerModuleId,
                Status = SignalEdgeStatus.Confirmed
            });
            await seed.SaveChangesAsync();
        }

        using (var resolver = new SignalEdgeResolver(
                   _fixture.CreateDbContext(),
                   new RippleProbeConsumerLookup(_fixture.CreateDbContext()),
                   Mock.Of<IPublishEndpoint>(),
                   NullLogger<SignalEdgeResolver>.Instance))
        {
            await resolver.ResolveAsModuleDestroyed(wave, _consumerModuleId, _organizationId);
        }

        await using var db = _fixture.CreateDbContext();
        var statuses = await db.SignalEdges
            .AsNoTracking()
            .Where(e => e.ProbeWaveId == wave && e.ConsumerModuleId == _consumerModuleId)
            .Select(e => e.Status)
            .ToListAsync();

        Assert.NotEmpty(statuses);
        Assert.All(statuses, status => Assert.Equal(SignalEdgeStatus.ModuleDestroyed, status));
    }

    /// <summary>
    /// The wave stops asking once the edge is resolved. Without this the saga reads another wave
    /// as owed and runs the probe branch again.
    /// </summary>
    [Fact]
    public async Task A_Destroyed_Module_Is_No_Longer_Owed_A_Probe()
    {
        var wave = await AddWave();

        await using (var seed = _fixture.CreateDbContext())
        {
            seed.SignalEdges.Add(new SignalEdge
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                ProbeWaveId = wave,
                ConsumerModuleId = _consumerModuleId,
                UpstreamModuleId = _producerModuleId,
                Status = SignalEdgeStatus.Confirmed
            });
            await seed.SaveChangesAsync();
        }

        await using (var before = _fixture.CreateDbContext())
        {
            using var builder = NewInputsBuilder(before);
            Assert.Contains(wave, await builder.FindWavesToProbeFor(_consumerModuleId, _organizationId));
        }

        using (var resolver = new SignalEdgeResolver(
                   _fixture.CreateDbContext(),
                   new RippleProbeConsumerLookup(_fixture.CreateDbContext()),
                   Mock.Of<IPublishEndpoint>(),
                   NullLogger<SignalEdgeResolver>.Instance))
        {
            await resolver.ResolveAsModuleDestroyed(wave, _consumerModuleId, _organizationId);
        }

        await using var after = _fixture.CreateDbContext();
        using var afterBuilder = NewInputsBuilder(after);
        Assert.DoesNotContain(wave, await afterBuilder.FindWavesToProbeFor(_consumerModuleId, _organizationId));
    }

    private static ProbeInputsBuilder NewInputsBuilder(SnapCdDbContext dbContext) =>
        new(dbContext,
            new RippleProbeConsumerLookup(dbContext),
            Mock.Of<ISignalSecretReader>(),
            NullLogger<ProbeInputsBuilder>.Instance);

    private async Task Resolve(Guid probeWaveId, Guid moduleId)
    {
        using var resolver = new SignalEdgeResolver(
            _fixture.CreateDbContext(),
            new RippleProbeConsumerLookup(_fixture.CreateDbContext()),
            Mock.Of<IPublishEndpoint>(),
            NullLogger<SignalEdgeResolver>.Instance);

        await resolver.ResolveAfterSignals(probeWaveId, moduleId, _organizationId);
    }


    /// <summary>
    /// The origin job whose approval a wave informs, and which it is constrained to. The job is
    /// awaiting that decision, which is the state a wave is live in.
    /// </summary>
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
            TimestampStart = DateTimeOffset.UtcNow.AddTicks(_originJobs.Count),
            CreatedDateTime = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        _originJobs.Add(jobId);
        return jobId;
    }

    private async Task<Guid> AddWave()
    {
        var wave = new ProbeWave
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            OriginModuleJobId = await AddOriginJob(_producerModuleId)
        };

        await using var db = _fixture.CreateDbContext();
        db.ProbeWaves.Add(wave);
        await db.SaveChangesAsync();

        _waves.Add(wave.Id);
        return wave.Id;
    }

    private async Task AddAttempt(Guid waveId, Guid moduleId)
    {
        await using var db = _fixture.CreateDbContext();
        db.ModuleProbeAttempts.Add(new ModuleProbeAttempt
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ProbeWaveId = waveId,
            ModuleId = moduleId
        });
        await db.SaveChangesAsync();
    }

    /// <summary>What a participant reported, with an action per output as a real plan carries.</summary>
    private async Task AddSignals(
        Guid waveId,
        Guid moduleId,
        params (string Name, string Action)[] outputs)
    {
        var set = new SignalOutputSet
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ModuleId = moduleId,
            ProbeWaveId = waveId,
            ModuleJobId = await AddOriginJob(moduleId),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Checksum = "test"
        };

        await using var db = _fixture.CreateDbContext();
        db.SignalOutputSets.Add(set);

        foreach (var (name, action) in outputs)
            db.LiteralSignalOutputs.Add(new LiteralSignalOutput
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                SignalOutputSetId = set.Id,
                Name = name,
                Sensitive = false,
                SignalJson = $"{{\"action\":[\"{action}\"],\"type\":\"string\"}}"
            });

        var attempt = await db.ModuleProbeAttempts
            .FirstAsync(a => a.ProbeWaveId == waveId && a.ModuleId == moduleId);
        attempt.SignalOutputSetId = set.Id;

        await db.SaveChangesAsync();
    }

    private async Task AddParamFromOutput(
        Guid moduleId,
        Guid outputModuleId,
        string outputName,
        string variableName)
    {
        var input = new ModuleParamFromOutput
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            OrganizationId = _organizationId,
            Name = variableName,
            OutputName = outputName,
            OutputModuleId = outputModuleId
        };

        await using var db = _fixture.CreateDbContext();
        db.ModuleInputs.Add(input);
        await db.SaveChangesAsync();

        _inputs.Add(input.Id);
    }
}
