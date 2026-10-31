// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
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
/// Whether an upstream's job should still be waiting for its consumers. A wave read as settled too
/// early sends findings in after the approval decision, which is the ordering the propagation
/// exists for; one read as waiting too long holds a job open for nothing.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ProbeWaveProgressReaderTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _organizationId;
    private Guid _originModuleId;
    private Guid _consumerModuleId;

    private readonly List<Guid> _waves = [];
    private readonly List<Guid> _originJobs = [];
    private readonly List<Guid> _signalSets = [];

    public ProbeWaveProgressReaderTests(Fixture fixture) => _fixture = fixture;

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
        await db.ModuleJobs.Where(j => _originJobs.Contains(j.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task A_Job_That_Started_No_Wave_Is_Not_Waiting()
    {
        var settlement = await Read(Guid.NewGuid());

        // The ordinary case for almost every apply, so this is the arm that must not park a job.
        Assert.Equal(ProbeWaveSettlement.NoWave, settlement);
    }

    [Fact]
    public async Task A_Consumer_With_An_Unresolved_Contribution_Keeps_The_Wave_Waiting()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddAttempt(wave, _originModuleId);
        await AddAttempt(wave, _consumerModuleId);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Potential);

        Assert.Equal(ProbeWaveSettlement.Waiting, await Read(jobId));
    }

    [Fact]
    public async Task A_Consumer_That_Owes_A_Probe_Keeps_The_Wave_Waiting()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddAttempt(wave, _originModuleId);
        await AddAttempt(wave, _consumerModuleId);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        // Resolved upstream and nothing reported yet: this is the state the sweep releases, so the
        // answer is still coming.
        Assert.Equal(ProbeWaveSettlement.Waiting, await Read(jobId));
    }

    [Fact]
    public async Task A_Consumer_That_Has_Reported_Settles_The_Wave()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddAttempt(wave, _originModuleId);
        var set = await AddSignalSet(wave, _consumerModuleId);
        await AddAttempt(wave, _consumerModuleId, set);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        Assert.Equal(ProbeWaveSettlement.Settled, await Read(jobId));
    }

    [Fact]
    public async Task A_Consumer_With_Nothing_Confirmed_Settles_The_Wave()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddAttempt(wave, _originModuleId);
        await AddAttempt(wave, _consumerModuleId);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.NotNeeded);

        // It has no probe to run, so it owes nobody an answer and the wave is done with it.
        Assert.Equal(ProbeWaveSettlement.Settled, await Read(jobId));
    }

    [Fact]
    public async Task An_Unanswerable_Contribution_Settles_The_Wave()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddAttempt(wave, _originModuleId);
        await AddAttempt(wave, _consumerModuleId);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Unanswerable);

        Assert.Equal(ProbeWaveSettlement.Settled, await Read(jobId));
    }

    /// <summary>
    /// A wave is its edges. A Module nothing points at is not in it, so a wave whose only rows are
    /// probe attempts has nothing outstanding.
    /// </summary>
    [Fact]
    public async Task A_Wave_With_No_Edges_Holds_Nothing_Open()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddAttempt(wave, _originModuleId);

        Assert.Equal(ProbeWaveSettlement.Settled, await Read(jobId));
    }

    [Fact]
    public async Task One_Consumer_Still_Waiting_Among_Several_Keeps_The_Wave_Waiting()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddAttempt(wave, _originModuleId);

        var reported = await AddSignalSet(wave, _consumerModuleId);
        await AddAttempt(wave, _consumerModuleId, reported);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        await AddAttempt(wave, _fixture.Modules["0002"].Id);
        await AddEdge(wave, _fixture.Modules["0002"].Id, _originModuleId, SignalEdgeStatus.Confirmed);

        Assert.Equal(ProbeWaveSettlement.Waiting, await Read(jobId));
    }

    [Fact]
    public async Task A_Wave_In_Another_Organization_Is_Not_Read()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddAttempt(wave, _originModuleId);
        await AddAttempt(wave, _consumerModuleId);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Potential);

        using var reader = new ProbeWaveProgressReader(_fixture.CreateDbContext());
        var settlement = await reader.ReadForOriginJob(jobId, _fixture.Organizations["1"].Id);

        Assert.Equal(ProbeWaveSettlement.NoWave, settlement);
    }

    private async Task<ProbeWaveSettlement> Read(Guid originJobId)
    {
        using var reader = new ProbeWaveProgressReader(_fixture.CreateDbContext());
        return await reader.ReadForOriginJob(originJobId, _organizationId);
    }

    private async Task<Guid> AddWave(Guid originJobId)
    {
        await AddOriginJob(originJobId, _originModuleId);

        var wave = new ProbeWave
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            OriginModuleJobId = originJobId
        };

        await using var db = _fixture.CreateDbContext();
        db.ProbeWaves.Add(wave);
        await db.SaveChangesAsync();

        _waves.Add(wave.Id);
        return wave.Id;
    }


    /// <summary>
    /// The origin job whose approval a wave informs, and which it is constrained to. The job is
    /// awaiting that decision, which is the state a wave is live in.
    /// </summary>
    private async Task<Guid> AddOriginJob(Guid jobId, Guid moduleId)
    {
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

    private async Task AddAttempt(
        Guid waveId,
        Guid moduleId,
        Guid? signalOutputSetId = null,
        Guid? probeModuleJobId = null)
    {
        await using var db = _fixture.CreateDbContext();
        db.ModuleProbeAttempts.Add(new ModuleProbeAttempt
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ProbeWaveId = waveId,
            ModuleId = moduleId,
            SignalOutputSetId = signalOutputSetId,
            ProbeModuleJobId = probeModuleJobId
        });
        await db.SaveChangesAsync();
    }

    private async Task AddEdge(
        Guid waveId,
        Guid consumerModuleId,
        Guid upstreamModuleId,
        SignalEdgeStatus status)
    {
        await using var db = _fixture.CreateDbContext();
        db.SignalEdges.Add(new SignalEdge
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ProbeWaveId = waveId,
            ConsumerModuleId = consumerModuleId,
            UpstreamModuleId = upstreamModuleId,
            Status = status
        });
        await db.SaveChangesAsync();
    }

    private async Task<Guid> AddSignalSet(Guid waveId, Guid moduleId)
    {
        var set = new SignalOutputSet
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ModuleId = moduleId,
            ProbeWaveId = waveId,
            ModuleJobId = await AddOriginJob(Guid.NewGuid(), moduleId),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Checksum = "test"
        };

        await using var db = _fixture.CreateDbContext();
        db.SignalOutputSets.Add(set);
        await db.SaveChangesAsync();

        _signalSets.Add(set.Id);
        return set.Id;
    }
}
