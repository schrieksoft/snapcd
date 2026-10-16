// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Entities.Definition.Signals;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services.RippleProbes;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.ProbeWaves;

/// <summary>
/// What a reader can see of a wave: who is in it, what each predicted, and what is deliberately
/// left out.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ProbeWaveReadServiceTests : IAsyncLifetime
{
    private const string UnknownAfter =
        """{"action":"unknown","type":"InN0cmluZyI=","before":"sGNoYW1waW9uLWhlcnJpbmc=","after":"1AAA"}""";

    private const string KnownUpdate =
        """{"action":"update","type":"InN0cmluZyI=","before":"pGxpdmU=","after":"pGRlYWQ="}""";

    private const string Created =
        """{"action":"create","type":"InN0cmluZyI=","after":"pGxpdmU="}""";

    private readonly Fixture _fixture;
    private Guid _organizationId;
    private Guid _originModuleId;
    private Guid _consumerModuleId;

    private readonly List<Guid> _waves = [];
    private readonly List<Guid> _signalSets = [];

    public ProbeWaveReadServiceTests(Fixture fixture) => _fixture = fixture;

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
    public async Task A_Wave_Reports_Its_Origin_And_Its_Participants()
    {
        var wave = await AddWave();
        await AddParticipant(wave, _originModuleId);
        await AddParticipant(wave, _consumerModuleId);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.NotNull(view);
        Assert.Equal(_originModuleId, view!.OriginModuleId);
        Assert.NotEmpty(view.OriginModuleName);
        Assert.Equal(2, view.Participants.Count);
        Assert.Equal(0, view.ParticipantsOmitted);
    }

    [Fact]
    public async Task A_Wave_In_Another_Organization_Is_Not_Returned()
    {
        var wave = await AddWave();
        await AddParticipant(wave, _originModuleId);

        using var service = Service();

        // The scoping is not a permission check, it is what keeps one tenant out of another's data.
        Assert.Null(await service.Get(wave, _fixture.Organizations["1"].Id));
    }

    [Fact]
    public async Task A_Participant_Whose_Module_Cannot_Be_Read_Is_Left_Out_And_Counted()
    {
        var wave = await AddWave();
        await AddParticipant(wave, _originModuleId);
        await AddParticipant(wave, _consumerModuleId);

        using var service = Service();
        var view = await service.Get(wave, _organizationId, moduleId => moduleId != _consumerModuleId);

        // Refusing the whole wave for one unreadable Module would hide the rest of a wave the
        // reader is entitled to; showing it would leak. Omitting and counting says both.
        Assert.NotNull(view);
        Assert.Single(view!.Participants);
        Assert.Equal(_originModuleId, view.Participants[0].ModuleId);
        Assert.Equal(1, view.ParticipantsOmitted);
    }

    [Fact]
    public async Task An_Unknown_New_Value_Is_Reported_As_Unknown()
    {
        var wave = await AddWave();
        var set = await AddSignalSet(wave, _consumerModuleId,
            ("pet_id", UnknownAfter, false),
            ("region", KnownUpdate, false));
        await AddParticipant(wave, _consumerModuleId, set);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        var signals = view!.Participants.Single().Signals;

        var unknown = signals.Single(s => s.Name == "pet_id");
        Assert.Equal("unknown", unknown.Action);
        Assert.True(unknown.AfterIsUnknown);
        Assert.True(unknown.HasBefore);

        // The distinction between "changing to something unknowable" and "changing to a known
        // value" is the whole finding, so the second signal must not read as unknown too.
        var known = signals.Single(s => s.Name == "region");
        Assert.Equal("update", known.Action);
        Assert.False(known.AfterIsUnknown);
    }

    [Fact]
    public async Task A_Created_Output_Has_No_Before()
    {
        var wave = await AddWave();
        var set = await AddSignalSet(wave, _consumerModuleId, ("new_id", Created, false));
        await AddParticipant(wave, _consumerModuleId, set);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        var signal = view!.Participants.Single().Signals.Single();
        Assert.Equal("create", signal.Action);
        Assert.False(signal.HasBefore);
        Assert.False(signal.AfterIsUnknown);
    }

    [Fact]
    public async Task A_Sensitive_Signal_Is_Named_But_Its_Value_Is_Not_Read()
    {
        var wave = await AddWave();
        var set = await AddSignalSet(wave, _consumerModuleId, ("token", UnknownAfter, true));
        await AddParticipant(wave, _consumerModuleId, set);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        var signal = view!.Participants.Single().Signals.Single();
        Assert.Equal("token", signal.Name);
        Assert.True(signal.Sensitive);

        // Its object lives in the secret store, so there is nothing here to describe and the view
        // does not go looking for it.
        Assert.Null(signal.Action);
    }

    [Fact]
    public async Task A_Participant_Waiting_On_An_Unresolved_Upstream_Reads_As_Waiting()
    {
        var wave = await AddWave();
        var upstream = await AddParticipant(wave, _originModuleId);
        var consumer = await AddParticipant(wave, _consumerModuleId);
        await AddContribution(consumer, upstream, SignalContributionStatus.Potential);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        var participant = view!.Participants.Single(p => p.ModuleId == _consumerModuleId);
        Assert.True(participant.IsWaiting);
        Assert.False(participant.HasProbed);
        Assert.False(view.IsSettled);

        var edge = participant.Contributions.Single();
        Assert.Equal(_originModuleId, edge.UpstreamModuleId);
        Assert.Equal(SignalContributionStatus.Potential, edge.Status);
    }

    [Fact]
    public async Task A_Wave_With_Everything_Resolved_Reads_As_Settled()
    {
        var wave = await AddWave();
        var upstream = await AddParticipant(wave, _originModuleId);
        var consumer = await AddParticipant(wave, _consumerModuleId);
        await AddContribution(consumer, upstream, SignalContributionStatus.NotNeeded);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.True(view!.IsSettled);
    }

    [Fact]
    public async Task The_Probe_Job_Is_Reported_So_Its_Logs_Can_Be_Found()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave();
        await AddParticipant(wave, _consumerModuleId, lastProbeModuleJobId: jobId);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.Equal(jobId, view!.Participants.Single().LastProbeModuleJobId);
    }

    [Fact]
    public async Task Waves_Are_Listed_For_The_Module_That_Started_Them()
    {
        var first = await AddWave();
        var second = await AddWave();
        await AddParticipant(first, _originModuleId);
        await AddParticipant(second, _originModuleId);

        using var service = Service();
        var waves = await service.ListByOriginModule(_originModuleId, _organizationId);

        Assert.Contains(waves, w => w.Id == first);
        Assert.Contains(waves, w => w.Id == second);
    }

    [Fact]
    public async Task Waves_Are_Listed_For_A_Module_That_Only_Takes_Part()
    {
        var wave = await AddWave();
        await AddParticipant(wave, _originModuleId);
        await AddParticipant(wave, _consumerModuleId);

        using var service = Service();
        var waves = await service.ListByParticipatingModule(_consumerModuleId, _organizationId);

        // The consumer did not start the wave, so listing by origin would not find it.
        Assert.Contains(waves, w => w.Id == wave);
    }

    private ProbeWaveReadService Service() => new(_fixture.CreateDbContext());

    private async Task<Guid> AddWave()
    {
        var wave = new ProbeWave
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            OriginModuleId = _originModuleId,
            OriginModuleJobId = Guid.NewGuid(),
            MaxDepth = 5
        };

        await using var db = _fixture.CreateDbContext();
        db.ProbeWaves.Add(wave);
        await db.SaveChangesAsync();

        _waves.Add(wave.Id);
        return wave.Id;
    }

    private async Task<Guid> AddParticipant(
        Guid waveId,
        Guid moduleId,
        Guid? signalOutputSetId = null,
        Guid? lastProbeModuleJobId = null)
    {
        var participant = new ProbeWaveParticipant
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ProbeWaveId = waveId,
            ModuleId = moduleId,
            SignalOutputSetId = signalOutputSetId,
            LastProbeModuleJobId = lastProbeModuleJobId
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

    private async Task<Guid> AddSignalSet(
        Guid waveId,
        Guid moduleId,
        params (string Name, string Json, bool Sensitive)[] signals)
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

        foreach (var (name, json, sensitive) in signals)
        {
            if (sensitive)
                db.SecretSignalOutputs.Add(new SecretSignalOutput
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _organizationId,
                    SignalOutputSetId = set.Id,
                    Name = name,
                    Sensitive = true,
                    RemoteSecretName = $"signal--{set.Id}--{name}"
                });
            else
                db.LiteralSignalOutputs.Add(new LiteralSignalOutput
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = _organizationId,
                    SignalOutputSetId = set.Id,
                    Name = name,
                    Sensitive = false,
                    SignalJson = json
                });
        }

        await db.SaveChangesAsync();

        _signalSets.Add(set.Id);
        return set.Id;
    }
}
