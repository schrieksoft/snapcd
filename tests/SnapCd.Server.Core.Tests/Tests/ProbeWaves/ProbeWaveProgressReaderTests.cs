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
using SnapCd.Server.Core.Services.RippleProbes;
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
        var origin = await AddParticipant(wave, _originModuleId);
        var consumer = await AddParticipant(wave, _consumerModuleId);
        await AddContribution(consumer, origin, SignalContributionStatus.Potential);

        Assert.Equal(ProbeWaveSettlement.Waiting, await Read(jobId));
    }

    [Fact]
    public async Task A_Consumer_That_Owes_A_Probe_Keeps_The_Wave_Waiting()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        var origin = await AddParticipant(wave, _originModuleId);
        var consumer = await AddParticipant(wave, _consumerModuleId);
        await AddContribution(consumer, origin, SignalContributionStatus.Confirmed);

        // Resolved upstream and nothing reported yet: this is the state the sweep releases, so the
        // answer is still coming.
        Assert.Equal(ProbeWaveSettlement.Waiting, await Read(jobId));
    }

    [Fact]
    public async Task A_Consumer_That_Has_Reported_Settles_The_Wave()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        var origin = await AddParticipant(wave, _originModuleId);
        var set = await AddSignalSet(wave, _consumerModuleId);
        var consumer = await AddParticipant(wave, _consumerModuleId, set);
        await AddContribution(consumer, origin, SignalContributionStatus.Confirmed);

        Assert.Equal(ProbeWaveSettlement.Settled, await Read(jobId));
    }

    [Fact]
    public async Task A_Consumer_With_Nothing_Confirmed_Settles_The_Wave()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        var origin = await AddParticipant(wave, _originModuleId);
        var consumer = await AddParticipant(wave, _consumerModuleId);
        await AddContribution(consumer, origin, SignalContributionStatus.NotNeeded);

        // It has no probe to run, so it owes nobody an answer and the wave is done with it.
        Assert.Equal(ProbeWaveSettlement.Settled, await Read(jobId));
    }

    [Fact]
    public async Task An_Unanswerable_Contribution_Settles_The_Wave()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        var origin = await AddParticipant(wave, _originModuleId);
        var consumer = await AddParticipant(wave, _consumerModuleId);
        await AddContribution(consumer, origin, SignalContributionStatus.Unanswerable);

        Assert.Equal(ProbeWaveSettlement.Settled, await Read(jobId));
    }

    [Fact]
    public async Task Participants_With_No_Contributions_Do_Not_Hold_The_Wave_Open()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddParticipant(wave, _originModuleId);
        await AddParticipant(wave, _fixture.Modules["0002"].Id);
        await AddParticipant(wave, _fixture.Modules["0003"].Id);

        // Participants are over-included from reachability, so most of a wide graph has no edges.
        // Counting those as outstanding would make every wave wait out its full timeout.
        Assert.Equal(ProbeWaveSettlement.Settled, await Read(jobId));
    }

    [Fact]
    public async Task One_Consumer_Still_Waiting_Among_Several_Keeps_The_Wave_Waiting()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        var origin = await AddParticipant(wave, _originModuleId);

        var reported = await AddSignalSet(wave, _consumerModuleId);
        var done = await AddParticipant(wave, _consumerModuleId, reported);
        await AddContribution(done, origin, SignalContributionStatus.Confirmed);

        var pending = await AddParticipant(wave, _fixture.Modules["0002"].Id);
        await AddContribution(pending, origin, SignalContributionStatus.Confirmed);

        Assert.Equal(ProbeWaveSettlement.Waiting, await Read(jobId));
    }

    [Fact]
    public async Task A_Wave_In_Another_Organization_Is_Not_Read()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        var origin = await AddParticipant(wave, _originModuleId);
        var consumer = await AddParticipant(wave, _consumerModuleId);
        await AddContribution(consumer, origin, SignalContributionStatus.Potential);

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
        var wave = new ProbeWave
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            OriginModuleId = _originModuleId,
            OriginModuleJobId = originJobId,
            MaxDepth = 5
        };

        await using var db = _fixture.CreateDbContext();
        db.ProbeWaves.Add(wave);
        await db.SaveChangesAsync();

        _waves.Add(wave.Id);
        return wave.Id;
    }

    private async Task<Guid> AddParticipant(Guid waveId, Guid moduleId, Guid? signalOutputSetId = null)
    {
        var participant = new ProbeWaveParticipant
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ProbeWaveId = waveId,
            ModuleId = moduleId,
            SignalOutputSetId = signalOutputSetId
        };

        await using var db = _fixture.CreateDbContext();
        db.ProbeWaveParticipants.Add(participant);
        await db.SaveChangesAsync();

        return participant.Id;
    }

    private async Task AddContribution(Guid participantId, Guid upstreamId, SignalContributionStatus status)
    {
        await using var db = _fixture.CreateDbContext();
        db.SignalContributions.Add(new SignalContribution
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ProbeWaveParticipantId = participantId,
            UpstreamParticipantId = upstreamId,
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
            ModuleJobId = Guid.NewGuid(),
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
