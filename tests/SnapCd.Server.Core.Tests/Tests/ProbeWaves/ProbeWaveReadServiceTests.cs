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
using SnapCd.Server.Core.Views;
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
        await db.SignalEdges.Where(e => _waves.Contains(e.ProbeWaveId)).ExecuteDeleteAsync();
        await db.ModuleProbeAttempts.Where(p => _waves.Contains(p.ProbeWaveId)).ExecuteDeleteAsync();
        await db.SignalOutputs.Where(o => _signalSets.Contains(o.SignalOutputSetId)).ExecuteDeleteAsync();
        await db.SignalOutputSets.Where(s => _signalSets.Contains(s.Id)).ExecuteDeleteAsync();
        await db.ProbeWaves.Where(w => _waves.Contains(w.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task A_Wave_Reports_Its_Origin_And_The_Modules_It_Reached()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);
        await AddEdge(wave, _fixture.Modules["0002"].Id, _originModuleId, SignalEdgeStatus.Potential);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.NotNull(view);
        Assert.Equal(_originModuleId, view!.OriginModuleId);
        Assert.NotEmpty(view.OriginModuleName);

        // The origin is not among them: it answers from its own plan rather than being probed.
        Assert.Equal(2, view.Modules.Count);
        Assert.DoesNotContain(_originModuleId, view.Modules.Select(m => m.ModuleId));
        Assert.Equal(0, view.ModulesOmitted);
    }

    [Fact]
    public async Task A_Wave_In_Another_Organization_Is_Not_Returned()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        using var service = Service();

        // The scoping is not a permission check, it is what keeps one tenant out of another's data.
        Assert.Null(await service.Get(wave, _fixture.Organizations["1"].Id));
    }

    [Fact]
    public async Task A_Module_The_Reader_Cannot_See_Is_Left_Out_And_Counted()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);
        await AddEdge(wave, _fixture.Modules["0002"].Id, _originModuleId, SignalEdgeStatus.Confirmed);

        using var service = Service();
        var view = await service.Get(wave, _organizationId, moduleId => moduleId != _consumerModuleId);

        // Refusing the whole wave for one unreadable Module would hide the rest of a wave the
        // reader is entitled to; showing it would leak. Omitting and counting says both.
        Assert.NotNull(view);
        Assert.Single(view!.Modules);
        Assert.Equal(_fixture.Modules["0002"].Id, view.Modules[0].ModuleId);
        Assert.Equal(1, view.ModulesOmitted);
    }

    [Fact]
    public async Task An_Unknown_New_Value_Is_Reported_As_Unknown()
    {
        var wave = await AddWave();
        var set = await AddSignalSet(wave, _consumerModuleId,
            ("pet_id", UnknownAfter, false),
            ("region", KnownUpdate, false));
        await AddAttempt(wave, _consumerModuleId, set);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        var signals = view!.Modules.Single().Signals;

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
        await AddAttempt(wave, _consumerModuleId, set);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        var signal = view!.Modules.Single().Signals.Single();
        Assert.Equal("create", signal.Action);
        Assert.False(signal.HasBefore);
        Assert.False(signal.AfterIsUnknown);
    }

    [Fact]
    public async Task A_Sensitive_Signal_Is_Named_But_Its_Value_Is_Not_Read()
    {
        var wave = await AddWave();
        var set = await AddSignalSet(wave, _consumerModuleId, ("token", UnknownAfter, true));
        await AddAttempt(wave, _consumerModuleId, set);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        var signal = view!.Modules.Single().Signals.Single();
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
        await AddAttempt(wave, _originModuleId);
        await AddAttempt(wave, _consumerModuleId);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Potential);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        var module = view!.Modules.Single(p => p.ModuleId == _consumerModuleId);
        Assert.True(module.IsWaiting);
        Assert.False(module.HasProbed);
        Assert.False(view.IsSettled);

        var edge = module.Edges.Single();
        Assert.Equal(_originModuleId, edge.UpstreamModuleId);
        Assert.Equal(SignalEdgeStatus.Potential, edge.Status);
    }

    [Fact]
    public async Task A_Wave_With_Everything_Resolved_Reads_As_Settled()
    {
        var wave = await AddWave();
        await AddAttempt(wave, _originModuleId);
        await AddAttempt(wave, _consumerModuleId);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.NotNeeded);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.True(view!.IsSettled);
    }

    [Fact]
    public async Task The_Probe_Job_Is_Reported_So_Its_Logs_Can_Be_Found()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave();
        await AddAttempt(wave, _consumerModuleId, probeModuleJobId: jobId);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.Equal(jobId, view!.Modules.Single().ProbeModuleJobId);
    }

    [Fact]
    public async Task Waves_Are_Listed_For_The_Module_That_Started_Them()
    {
        var first = await AddWave();
        var second = await AddWave();
        await AddAttempt(first, _originModuleId);
        await AddAttempt(second, _originModuleId);

        using var service = Service();
        var waves = await service.ListByOriginModule(_originModuleId, _organizationId);

        Assert.Contains(waves, w => w.Id == first);
        Assert.Contains(waves, w => w.Id == second);
    }

    [Fact]
    public async Task Waves_Are_Listed_For_A_Module_That_Only_Takes_Part()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        using var service = Service();
        var waves = await service.ListByParticipatingModule(_consumerModuleId, _organizationId);

        // The consumer did not start the wave, so listing by origin would not find it.
        Assert.Contains(waves, w => w.Id == wave);
    }

    [Fact]
    public async Task A_Job_That_Started_No_Wave_Reads_As_Null()
    {
        Assert.Null(await Service().GetByOriginJob(Guid.NewGuid(), _organizationId));
    }

    [Fact]
    public async Task A_Wave_Is_Found_By_The_Job_That_Started_It()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddAttempt(wave, _originModuleId);

        var found = await Service().GetByOriginJob(jobId, _organizationId);

        Assert.NotNull(found);
        Assert.Equal(wave, found!.Id);
    }

    [Fact]
    public async Task A_Wave_Is_Not_Found_From_Another_Organization()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddAttempt(wave, _originModuleId);

        Assert.Null(await Service().GetByOriginJob(jobId, _fixture.Organizations["1"].Id));
    }

    [Fact]
    public async Task A_Participant_Waiting_On_An_Upstream_Reads_As_Waiting()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddAttempt(wave, _originModuleId);
        await AddAttempt(wave, _consumerModuleId);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Potential);

        var found = await Service().GetByOriginJob(jobId, _organizationId);
        var module = found!.Modules.Single(p => p.ModuleId == _consumerModuleId);

        Assert.Equal(ProbeProgress.WaitingOnUpstream, module.Progress);
    }

    [Fact]
    public async Task A_Released_Participant_With_No_Job_Yet_Reads_As_Queued()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddAttempt(wave, _originModuleId);
        await AddAttempt(wave, _consumerModuleId);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        var found = await Service().GetByOriginJob(jobId, _organizationId);
        var module = found!.Modules.Single(p => p.ModuleId == _consumerModuleId);

        // The sweep has not started its Apply yet, which is a real state a reader sees for up to
        // one sweep interval.
        Assert.Equal(ProbeProgress.Queued, module.Progress);
    }

    [Fact]
    public async Task A_Participant_Whose_Probe_Job_Is_Running_Reads_As_Running()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddAttempt(wave, _originModuleId);
        await AddAttempt(wave, _consumerModuleId, probeModuleJobId: Guid.NewGuid());
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        var found = await Service().GetByOriginJob(jobId, _organizationId);
        var module = found!.Modules.Single(p => p.ModuleId == _consumerModuleId);

        Assert.Equal(ProbeProgress.Running, module.Progress);
    }

    [Fact]
    public async Task A_Participant_That_Reported_Reads_As_Reported()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddAttempt(wave, _originModuleId);
        var set = await AddSignalSet(wave, _consumerModuleId);
        await AddAttempt(wave, _consumerModuleId, set);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        var found = await Service().GetByOriginJob(jobId, _organizationId);
        var module = found!.Modules.Single(p => p.ModuleId == _consumerModuleId);

        Assert.Equal(ProbeProgress.Reported, module.Progress);
    }

    [Fact]
    public async Task A_Participant_Nothing_Reaches_Reads_As_Nothing_To_Probe()
    {
        var jobId = Guid.NewGuid();
        var wave = await AddWave(jobId);
        await AddAttempt(wave, _originModuleId);
        await AddAttempt(wave, _consumerModuleId);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.NotNeeded);

        var found = await Service().GetByOriginJob(jobId, _organizationId);
        var module = found!.Modules.Single(p => p.ModuleId == _consumerModuleId);

        // Participants come from reachability, so most of a wide graph lands here and the panel
        // list must not show them as pending work.
        Assert.Equal(ProbeProgress.NothingToProbe, module.Progress);
    }

    private ProbeWaveReadService Service() => new(_fixture.CreateDbContext());

    private async Task<Guid> AddWave(Guid? originModuleJobId = null)
    {
        var wave = new ProbeWave
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            OriginModuleId = _originModuleId,
            OriginModuleJobId = originModuleJobId ?? Guid.NewGuid()
        };

        await using var db = _fixture.CreateDbContext();
        db.ProbeWaves.Add(wave);
        await db.SaveChangesAsync();

        _waves.Add(wave.Id);
        return wave.Id;
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
