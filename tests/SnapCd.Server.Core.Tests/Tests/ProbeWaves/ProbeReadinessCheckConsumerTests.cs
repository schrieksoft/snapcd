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
using SnapCd.Server.Core.Consumers.System.Competing;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.RippleProbes;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.ProbeWaves;

/// <summary>
/// Whether a readiness check starts a participant's Apply. The check is what makes a released
/// participant probe, so one it declines wrongly is a wave that stalls, and one it accepts too
/// early is an Apply nobody asked for.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ProbeReadinessCheckConsumerTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _organizationId;
    private Guid _upstreamModuleId;
    private Guid _consumerModuleId;

    private readonly List<Guid> _waves = [];
    private readonly List<Guid> _signalSets = [];
    private readonly List<Guid> _jobs = [];

    public ProbeReadinessCheckConsumerTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _organizationId = _fixture.Organizations["0"].Id;
        _upstreamModuleId = _fixture.Modules["0000"].Id;
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
        await db.ModuleJobs.Where(j => _jobs.Contains(j.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task A_Participant_Still_Waiting_Is_Not_Released()
    {
        var wave = await AddWave();
        var upstream = await AddParticipant(wave, _upstreamModuleId);
        var consumer = await AddParticipant(wave, _consumerModuleId);
        await AddContribution(consumer, upstream, SignalContributionStatus.Potential);

        Assert.False(await Check(consumer, _consumerModuleId));
    }

    [Fact]
    public async Task A_Participant_With_Everything_Resolved_And_One_Confirmed_Is_Released()
    {
        var wave = await AddWave();
        var upstream = await AddParticipant(wave, _upstreamModuleId);
        var consumer = await AddParticipant(wave, _consumerModuleId);
        await AddContribution(consumer, upstream, SignalContributionStatus.Confirmed);

        Assert.True(await Check(consumer, _consumerModuleId));
    }

    [Fact]
    public async Task A_Participant_With_Nothing_Confirmed_Is_Left_Alone()
    {
        var wave = await AddWave();
        var upstream = await AddParticipant(wave, _upstreamModuleId);
        var consumer = await AddParticipant(wave, _consumerModuleId);
        await AddContribution(consumer, upstream, SignalContributionStatus.NotNeeded);

        // Nothing to probe for, so applying it would be a side effect rather than a probe.
        Assert.False(await Check(consumer, _consumerModuleId));
    }

    [Fact]
    public async Task An_Unanswerable_Upstream_Still_Releases_A_Confirmed_Consumer()
    {
        var wave = await AddWave();
        var confirmed = await AddParticipant(wave, _upstreamModuleId);
        var gone = await AddParticipant(wave, _fixture.Modules["0002"].Id);
        var consumer = await AddParticipant(wave, _consumerModuleId);
        await AddContribution(consumer, confirmed, SignalContributionStatus.Confirmed);
        await AddContribution(consumer, gone, SignalContributionStatus.Unanswerable);

        // Unanswerable resolves rather than holds, so one upstream that will never report does
        // not strand a consumer that has a real signal waiting for it.
        Assert.True(await Check(consumer, _consumerModuleId));
    }

    [Fact]
    public async Task A_Participant_That_Already_Probed_Is_Not_Released_Again()
    {
        var wave = await AddWave();
        var upstream = await AddParticipant(wave, _upstreamModuleId);
        var set = await AddSignalSet(wave, _consumerModuleId);
        var consumer = await AddParticipant(wave, _consumerModuleId, set);
        await AddContribution(consumer, upstream, SignalContributionStatus.Confirmed);

        Assert.False(await Check(consumer, _consumerModuleId));
    }

    [Fact]
    public async Task A_Module_Already_Running_A_Job_Is_Not_Released()
    {
        var wave = await AddWave();
        var upstream = await AddParticipant(wave, _upstreamModuleId);
        var consumer = await AddParticipant(wave, _consumerModuleId);
        await AddContribution(consumer, upstream, SignalContributionStatus.Confirmed);
        await AddJob(_consumerModuleId, ExecutionStatus.Running);

        // Its probe step reads the contributions itself, so a second Apply would race the first.
        Assert.False(await Check(consumer, _consumerModuleId));
    }

    [Fact]
    public async Task A_Wave_Whose_Origin_Job_Stopped_Releases_Nobody()
    {
        var wave = await AddWave(originRunning: false);
        var upstream = await AddParticipant(wave, _upstreamModuleId);
        var consumer = await AddParticipant(wave, _consumerModuleId);
        await AddContribution(consumer, upstream, SignalContributionStatus.Confirmed);

        // Cancelling the upstream stops the wave producing new jobs: nobody is waiting on the
        // answer any more.
        Assert.False(await Check(consumer, _consumerModuleId));
    }

    [Fact]
    public async Task A_Participant_With_No_Contributions_Is_Not_Released()
    {
        var wave = await AddWave();
        var orphan = await AddParticipant(wave, _consumerModuleId);

        // Participants are over-included from reachability, so most of a wide graph has no edges
        // and must not be applied.
        Assert.False(await Check(orphan, _consumerModuleId));
    }

    /// <summary>
    /// Runs one check and reports whether it started an Apply, which is the only observable the
    /// consumer has.
    /// </summary>
    private async Task<bool> Check(Guid participantId, Guid moduleId)
    {
        var factory = new FixtureDbContextFactory(_fixture);
        var jobService = new Mock<JobService>(
            Mock.Of<IBus>(), _fixture.CreateDbContext(), null!, null!, null!, null!,
            factory, null!, null!, null!) { CallBase = false };

        var started = false;
        jobService
            .Setup(x => x.Apply(moduleId, _organizationId, It.IsAny<Guid?>(), null, null))
            .Callback(() => started = true)
            .Returns(Task.CompletedTask);

        var consumer = new ProbeReadinessCheckCompetingConsumer(
            NullLogger<ProbeReadinessCheckCompetingConsumer>.Instance,
            new ProbeWaveProgressReaderFactory(factory),
            factory,
            jobService.Object,
            null!);

        await consumer.Consume(ConsumeContextFor(new ProbeReadinessCheckRequested
        {
            ProbeWaveParticipantId = participantId,
            ModuleId = moduleId,
            OrganizationId = _organizationId
        }));

        return started;
    }

    private static ConsumeContext<T> ConsumeContextFor<T>(T message) where T : class
    {
        var context = new Mock<ConsumeContext<T>>();
        context.SetupGet(x => x.Message).Returns(message);
        return context.Object;
    }

    private sealed class FixtureDbContextFactory(Fixture fixture) : IDbContextFactory<SnapCdDbContext>
    {
        public SnapCdDbContext CreateDbContext() => fixture.CreateDbContext();
    }

    /// <summary>
    /// A wave with its origin job still running, which is the only state the sweep starts work
    /// for. An origin that has stopped has nobody waiting on the answer.
    /// </summary>
    private async Task<Guid> AddWave(bool originRunning = true)
    {
        var originJobId = await AddJob(
            _upstreamModuleId,
            originRunning ? ExecutionStatus.Running : ExecutionStatus.Cancelled);

        var wave = new ProbeWave
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            OriginModuleId = _upstreamModuleId,
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

    private async Task<Guid> AddJob(Guid moduleId, ExecutionStatus status)
    {
        var job = new ModuleJob
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ModuleId = moduleId,
            Status = status,
            JobType = "ApplyJobSaga"
        };

        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(job);
        await db.SaveChangesAsync();

        _jobs.Add(job.Id);
        return job.Id;
    }
}
