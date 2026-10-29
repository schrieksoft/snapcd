// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Entities.Definition.Signals;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services.Nonsecured.RippleProbes;
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
        """{"action":["update"],"type":"string","before":"champion-herring","after":{"$unknown":true}}""";

    private const string KnownUpdate =
        """{"action":["update"],"type":"string","before":"live","after":"dead"}""";

    private const string Created =
        """{"action":["create"],"type":"string","after":"live"}""";

    private const string Replaced =
        """{"action":["delete","create"],"type":"string","before":"live","after":{"$unknown":true}}""";

    private const string NoOp =
        """{"action":["no-op"],"type":"string","before":"52.247.5.27","after":"52.247.5.27"}""";

    private readonly Fixture _fixture;
    private Guid _organizationId;
    private Guid _originModuleId;
    private Guid _consumerModuleId;

    private readonly List<Guid> _waves = [];
    private readonly List<Guid> _signalSets = [];
    private readonly List<Guid> _jobs = [];

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
        await db.ModuleJobs.Where(j => _jobs.Contains(j.Id)).ExecuteDeleteAsync();
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
        Assert.Equal("update", unknown.Action);
        Assert.True(unknown.AfterIsUnknown);
        Assert.True(unknown.HasBefore);

        // The distinction between "changing to something unknowable" and "changing to a known
        // value" is the whole finding, so the second signal must not read as unknown too.
        var known = signals.Single(s => s.Name == "region");
        Assert.Equal("update", known.Action);
        Assert.False(known.AfterIsUnknown);
    }

    [Fact]
    public async Task Every_Probe_A_Module_Ran_Is_Reported_Newest_First()
    {
        var wave = await AddWave();

        var first = await AddSignalSet(wave, _consumerModuleId, ("pet_id", KnownUpdate, false));
        var firstJob = Guid.NewGuid();
        await AddAttempt(wave, _consumerModuleId, first, firstJob, timestamp: 100);

        var second = await AddSignalSet(wave, _consumerModuleId, ("pet_id", UnknownAfter, false));
        var secondJob = Guid.NewGuid();
        await AddAttempt(wave, _consumerModuleId, second, secondJob, timestamp: 200);

        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        var module = view!.Modules.Single();

        Assert.Equal(2, module.Attempts.Count);
        Assert.Equal(secondJob, module.Attempts[0].ProbeModuleJobId);
        Assert.Equal(firstJob, module.Attempts[1].ProbeModuleJobId);

        // Each attempt keeps its own prediction, so a re-probe does not rewrite what the earlier
        // one said.
        Assert.True(module.Attempts[0].Signals.Single().AfterIsUnknown);
        Assert.False(module.Attempts[1].Signals.Single().AfterIsUnknown);

        // The approval decision reads the newest, which is what the badge shows.
        Assert.Equal(secondJob, module.ProbeModuleJobId);
        Assert.True(module.Signals.Single().AfterIsUnknown);
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

    // Every output is carried, changed or not, because a consumer needs a value for each variable
    // it reads. A module whose outputs all resolve unchanged therefore has signals and moves
    // nothing, and counting the rows reports it as changing.
    [Fact]
    public async Task Signals_That_All_Hold_Still_Are_Not_Movement()
    {
        var wave = await AddWave();
        var set = await AddSignalSet(
            wave,
            _consumerModuleId,
            ("public_gateway_ip", NoOp, false),
            ("public_gateway_fqdn", NoOp, false));
        await AddAttempt(wave, _consumerModuleId, set);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        var module = view!.Modules.Single();

        Assert.Equal(2, module.Signals.Count);
        Assert.Empty(module.MovingSignals);
        Assert.False(module.HasDisruptiveSignal);
        Assert.False(module.HasUnknownSignal);

        Assert.Equal(DownstreamImpact.Cleared, view.Impact);
    }

    [Fact]
    public async Task A_Signal_That_Moves_Among_Ones_That_Do_Not_Is_Movement()
    {
        var wave = await AddWave();
        var set = await AddSignalSet(
            wave,
            _consumerModuleId,
            ("public_gateway_ip", NoOp, false),
            ("cluster_endpoint", KnownUpdate, false));
        await AddAttempt(wave, _consumerModuleId, set);
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        var module = view!.Modules.Single();

        Assert.Equal(2, module.Signals.Count);
        Assert.Equal("cluster_endpoint", module.MovingSignals.Single().Name);

        Assert.Equal(DownstreamImpact.Changes, view.Impact);
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


    /// <summary>
    /// A wave is pending from the moment it exists. Its edges are all Potential at that point,
    /// and reading cleared-ness off the edges alone would call it finished before anything ran.
    /// </summary>
    [Fact]
    public async Task A_Wave_Whose_Consumers_Have_Not_Answered_Is_Pending()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Potential);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.Equal(DownstreamImpact.Pending, view!.Impact);
    }

    /// <summary>
    /// The case the edge statuses alone get wrong. A wave spreads as each Module reports, so
    /// between one Module resolving and its own consumers being enrolled there is no Potential
    /// edge anywhere - and the Modules it will reach are not in the table yet. What holds the
    /// answer open is the Module that owes a probe, not the edges into it.
    /// </summary>
    [Fact]
    public async Task A_Confirmed_Module_That_Has_Not_Probed_Keeps_The_Wave_Pending()
    {
        var wave = await AddWave();

        // Every edge has resolved, so nothing is Potential, and nothing downstream of this
        // consumer exists yet because it has not reported.
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.Equal(DownstreamImpact.Pending, view!.Impact);
    }

    [Fact]
    public async Task A_Wave_Whose_Consumers_All_Reported_An_Update_Reports_Changes()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);
        var set = await AddSignalSet(wave, _consumerModuleId, ("id", KnownUpdate, false));
        await AddAttempt(wave, _consumerModuleId, set);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.Equal(DownstreamImpact.Changes, view!.Impact);
    }

    /// <summary>A consumer that reported no signal at all is the only thing that clears a wave.</summary>
    [Fact]
    public async Task A_Wave_Whose_Consumers_Reported_Nothing_Is_Cleared()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);
        var set = await AddSignalSet(wave, _consumerModuleId);
        await AddAttempt(wave, _consumerModuleId, set);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.Equal(DownstreamImpact.Cleared, view!.Impact);
    }

    /// <summary>
    /// A Module nothing reaches has no probe to run, so it cannot be what the wave is waiting on.
    /// </summary>
    [Fact]
    public async Task A_Module_Nothing_Reaches_Does_Not_Hold_The_Wave_Open()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.NotNeeded);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.Equal(DownstreamImpact.Cleared, view!.Impact);
    }

    [Fact]
    public async Task A_Predicted_Replace_Downstream_Reports_An_Impact()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);
        var set = await AddSignalSet(wave, _consumerModuleId, ("id", Replaced, false));
        await AddAttempt(wave, _consumerModuleId, set);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.Equal(DownstreamImpact.Impact, view!.Impact);
    }

    /// <summary>
    /// An impact is worth reporting as soon as one Module predicts it, rather than waiting for the
    /// rest of the wave: it is the finding a reader might stop for.
    /// </summary>
    [Fact]
    public async Task An_Impact_Is_Reported_While_Other_Modules_Are_Still_Running()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);
        var set = await AddSignalSet(wave, _consumerModuleId, ("id", Replaced, false));
        await AddAttempt(wave, _consumerModuleId, set);

        var stillRunning = _fixture.Modules["0002"].Id;
        await AddEdge(wave, stillRunning, _originModuleId, SignalEdgeStatus.Confirmed);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.Equal(DownstreamImpact.Impact, view!.Impact);
    }

    /// <summary>
    /// A probe whose job died reports no signals and never will, so the wave's answer is
    /// incomplete in a way that waiting does not fix.
    /// </summary>
    [Fact]
    public async Task A_Probe_Whose_Job_Failed_Reports_A_Failure()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);
        var jobId = await AddJob(_consumerModuleId, ExecutionStatus.Failed);
        await AddAttempt(wave, _consumerModuleId, probeModuleJobId: jobId);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.Equal(DownstreamImpact.Failed, view!.Impact);
        Assert.Equal(ProbeProgress.Faulted, view.Modules.Single().Progress);
    }

    /// <summary>
    /// A probe that failed and was then run again by a later Apply reports what the second
    /// attempt found. The wave keeps both rows, and the newest one is the Module's answer.
    /// </summary>
    [Fact]
    public async Task A_Reprobed_Module_Reports_Its_Latest_Attempt()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);

        var failedJob = await AddJob(_consumerModuleId, ExecutionStatus.Failed);
        await AddAttempt(wave, _consumerModuleId, probeModuleJobId: failedJob, timestamp: 1);

        var secondJob = await AddJob(_consumerModuleId, ExecutionStatus.Completed);
        var set = await AddSignalSet(wave, _consumerModuleId);
        await AddAttempt(wave, _consumerModuleId, signalOutputSetId: set, probeModuleJobId: secondJob, timestamp: 2);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.Equal(DownstreamImpact.Cleared, view!.Impact);

        var module = view.Modules.Single();
        Assert.True(module.HasProbed);
        Assert.Equal(secondJob, module.ProbeModuleJobId);
    }

    /// <summary>
    /// A failure outranks work still in flight. Waiting for the rest would leave the reader
    /// watching a wave that can no longer answer in full.
    /// </summary>
    [Fact]
    public async Task A_Failure_Outranks_Modules_Still_Running()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);
        var jobId = await AddJob(_consumerModuleId, ExecutionStatus.Failed);
        await AddAttempt(wave, _consumerModuleId, probeModuleJobId: jobId);

        var stillWaiting = _fixture.Modules["0002"].Id;
        await AddEdge(wave, stillWaiting, _originModuleId, SignalEdgeStatus.Potential);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.Equal(DownstreamImpact.Failed, view!.Impact);
    }

    /// <summary>
    /// A probe whose job finished without recording anything reads as failed, not as running. The
    /// prediction is not coming, and showing it as in flight holds the wave open on a job that has
    /// already stopped.
    /// </summary>
    [Fact]
    public async Task A_Probe_Whose_Job_Finished_Without_Recording_Is_Not_Still_Running()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);
        var jobId = await AddJob(_consumerModuleId, ExecutionStatus.Failed);
        await AddAttempt(wave, _consumerModuleId, probeModuleJobId: jobId);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        var module = view!.Modules.Single();

        Assert.Equal(ProbeProgress.Faulted, module.Progress);
        Assert.False(module.IsOutstanding);
        Assert.Equal(DownstreamImpact.Failed, view.Impact);
    }

    /// <summary>
    /// A probe that predicted nothing still answered: the recorder writes a set holding no outputs,
    /// so every reader testing for one sees that this Module is done.
    /// </summary>
    [Fact]
    public async Task A_Probe_That_Predicted_Nothing_Has_Reported()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);
        var jobId = await AddJob(_consumerModuleId, ExecutionStatus.Completed);
        var set = await AddSignalSet(wave, _consumerModuleId);
        await AddAttempt(wave, _consumerModuleId, set, jobId);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        var module = view!.Modules.Single();

        Assert.Equal(ProbeProgress.Reported, module.Progress);
        Assert.Empty(module.Signals);
        Assert.False(module.IsOutstanding);
        Assert.Equal(DownstreamImpact.Cleared, view.Impact);
    }

    /// <summary>
    /// A probe that is genuinely still running is not a failure. Its job is Running, which is the
    /// state every probe passes through.
    /// </summary>
    [Fact]
    public async Task A_Probe_Still_Running_Is_Pending_Rather_Than_Failed()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);
        var jobId = await AddJob(_consumerModuleId, ExecutionStatus.Running);
        await AddAttempt(wave, _consumerModuleId, probeModuleJobId: jobId);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.Equal(DownstreamImpact.Pending, view!.Impact);
        Assert.Equal(ProbeProgress.Running, view.Modules.Single().Progress);
    }

    /// <summary>
    /// A probe that reported and then had its job cancelled still answered. The signals are
    /// recorded, so the finding stands.
    /// </summary>
    [Fact]
    public async Task A_Probe_That_Reported_Before_Its_Job_Ended_Still_Counts_As_Reported()
    {
        var wave = await AddWave();
        await AddEdge(wave, _consumerModuleId, _originModuleId, SignalEdgeStatus.Confirmed);
        var jobId = await AddJob(_consumerModuleId, ExecutionStatus.Cancelled);
        var set = await AddSignalSet(wave, _consumerModuleId, ("id", KnownUpdate, false));
        await AddAttempt(wave, _consumerModuleId, set, jobId);

        using var service = Service();
        var view = await service.Get(wave, _organizationId);

        Assert.Equal(DownstreamImpact.Changes, view!.Impact);
        Assert.Equal(ProbeProgress.Reported, view.Modules.Single().Progress);
    }

    private async Task<Guid> AddJob(Guid moduleId, ExecutionStatus status, Guid? id = null)
    {
        // ModuleJobs is unique on (ModuleId, TimestampStart, OrganizationId), so a Module probed
        // twice needs two distinct start times.
        var job = new ModuleJob
        {
            Id = id ?? Guid.NewGuid(),
            OrganizationId = _organizationId,
            ModuleId = moduleId,
            Status = status,
            TimestampStart = DateTimeOffset.UtcNow.AddTicks(_jobs.Count),
            JobType = "ApplyJobSaga"
        };

        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(job);
        await db.SaveChangesAsync();

        _jobs.Add(job.Id);
        return job.Id;
    }

    /// <summary>
    /// A wave and the origin job whose approval it informs. A wave is constrained to that job, so
    /// one cannot be written without it; a caller naming an id gets a job under that id.
    /// </summary>
    private async Task<Guid> AddWave(Guid? originModuleJobId = null)
    {
        var wave = new ProbeWave
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            OriginModuleJobId = await AddJob(
                _originModuleId, ExecutionStatus.Running, originModuleJobId)
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
        Guid? probeModuleJobId = null,
        long timestamp = 1)
    {
        await using var db = _fixture.CreateDbContext();
        db.ModuleProbeAttempts.Add(new ModuleProbeAttempt
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ProbeWaveId = waveId,
            ModuleId = moduleId,
            SignalOutputSetId = signalOutputSetId,
            ProbeModuleJobId = probeModuleJobId,
            Timestamp = timestamp
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
