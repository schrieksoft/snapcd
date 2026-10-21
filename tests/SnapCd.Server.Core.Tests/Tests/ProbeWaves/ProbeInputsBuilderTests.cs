// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Entities.Definition.Signals;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services.RippleProbes;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.ProbeWaves;

/// <summary>
/// Whether a Module has a probe to run, and what the inputs document it probes with contains. The
/// document's value objects are planwalker's own and are copied rather than rebuilt, so the
/// assertions are on the bytes as much as on the keys.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ProbeInputsBuilderTests : IAsyncLifetime
{
    private const string VpcSignal =
        """{"action":"update","type":"InN0cmluZyI=","before":"sGNoYW1waW9uLWhlcnJpbmc=","after":"1AAA"}""";

    private const string OtherSignal =
        """{"action":"create","type":"InN0cmluZyI=","after":"pGxpdmU="}""";

    private readonly Fixture _fixture;
    private Guid _organizationId;
    private Guid _consumerId;
    private Guid _upstreamAId;
    private Guid _upstreamBId;

    private readonly List<Guid> _waves = [];
    private readonly List<Guid> _signalSets = [];
    private readonly List<Guid> _inputs = [];
    private readonly List<Guid> _variableSets = [];

    public ProbeInputsBuilderTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _organizationId = _fixture.Organizations["0"].Id;
        _consumerId = _fixture.Modules["0000"].Id;
        _upstreamAId = _fixture.Modules["0001"].Id;
        _upstreamBId = _fixture.Modules["0002"].Id;
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
        await db.ModuleInputs.Where(i => _inputs.Contains(i.Id)).ExecuteDeleteAsync();
        await db.VariableSets.Where(v => _variableSets.Contains(v.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task A_Module_With_No_Wave_Has_Nothing_To_Probe()
    {
        using var builder = Builder();

        Assert.Empty(await builder.FindWavesToProbeFor(_consumerId, _organizationId));
    }

    [Fact]
    public async Task A_Potential_Contribution_Is_Not_Enough_To_Probe()
    {
        var wave = await AddWave();
        await AddAttempt(wave, _upstreamAId);
        await AddAttempt(wave, _consumerId);
        await AddEdge(wave, _consumerId, _upstreamAId, SignalEdgeStatus.Potential);

        using var builder = Builder();

        Assert.Empty(await builder.FindWavesToProbeFor(_consumerId, _organizationId));
    }

    [Fact]
    public async Task A_Confirmed_Contribution_Means_There_Is_A_Probe_To_Run()
    {
        var wave = await AddWave();
        await AddAttempt(wave, _upstreamAId);
        await AddAttempt(wave, _consumerId);
        await AddEdge(wave, _consumerId, _upstreamAId, SignalEdgeStatus.Confirmed);

        using var builder = Builder();

        Assert.Equal(wave, Assert.Single(await builder.FindWavesToProbeFor(_consumerId, _organizationId)));
    }

    /// <summary>
    /// Two waves from different origins can be open on one Module at once. Each predicts
    /// different values for its inputs, so each is owed a probe of its own: one answer shared
    /// between them would describe a future that does not exist for either.
    /// </summary>
    [Fact]
    public async Task A_Module_In_Two_Waves_Owes_A_Probe_To_Each()
    {
        var first = await AddWave();
        await AddAttempt(first, _upstreamAId);
        await AddEdge(first, _consumerId, _upstreamAId, SignalEdgeStatus.Confirmed);

        var second = await AddWave();
        await AddAttempt(second, _upstreamBId);
        await AddEdge(second, _consumerId, _upstreamBId, SignalEdgeStatus.Confirmed);

        using var builder = Builder();

        var waves = await builder.FindWavesToProbeFor(_consumerId, _organizationId);

        Assert.Equal(2, waves.Count);
        Assert.Contains(first, waves);
        Assert.Contains(second, waves);
    }

    /// <summary>
    /// Having answered one of them leaves the other still owed, which is what makes the probe
    /// step loop terminate.
    /// </summary>
    [Fact]
    public async Task Answering_One_Wave_Leaves_The_Other_Owed()
    {
        var first = await AddWave();
        var set = await AddSignalSet(first, _consumerId, ("done", VpcSignal, false));
        await AddAttempt(first, _upstreamAId);
        await AddAttempt(first, _consumerId, set);
        await AddEdge(first, _consumerId, _upstreamAId, SignalEdgeStatus.Confirmed);

        var second = await AddWave();
        await AddAttempt(second, _upstreamBId);
        await AddEdge(second, _consumerId, _upstreamBId, SignalEdgeStatus.Confirmed);

        using var builder = Builder();

        Assert.Equal(second, Assert.Single(await builder.FindWavesToProbeFor(_consumerId, _organizationId)));
    }

    [Fact]
    public async Task A_Participant_That_Already_Answered_Is_Not_Asked_Again()
    {
        var wave = await AddWave();
        var set = await AddSignalSet(wave, _consumerId, ("done", VpcSignal, false));
        await AddAttempt(wave, _upstreamAId);
        await AddAttempt(wave, _consumerId, set);
        await AddEdge(wave, _consumerId, _upstreamAId, SignalEdgeStatus.Confirmed);

        using var builder = Builder();

        // Its signal set is the record that it has answered, so a later apply of the same Module
        // is not this wave's business.
        Assert.Empty(await builder.FindWavesToProbeFor(_consumerId, _organizationId));
    }

    [Fact]
    public async Task Nothing_To_Wait_For_When_No_Wave_Wants_This_Module()
    {
        using var reader = Readiness();

        Assert.Equal(ProbeReadiness.NothingToWaitFor, await reader.Read(_consumerId, _organizationId));
    }

    [Fact]
    public async Task An_Unresolved_Upstream_Means_Wait()
    {
        var wave = await AddWave();
        await AddAttempt(wave, _upstreamAId);
        await AddAttempt(wave, _upstreamBId);
        await AddAttempt(wave, _consumerId);
        await AddEdge(wave, _consumerId, _upstreamAId, SignalEdgeStatus.Confirmed);
        await AddEdge(wave, _consumerId, _upstreamBId, SignalEdgeStatus.Potential);

        using var reader = Readiness();

        // One confirmed is not enough while another may still confirm: probing now would plan
        // with half the signals.
        Assert.Equal(ProbeReadiness.WaitForSignals, await reader.Read(_consumerId, _organizationId));
    }

    [Fact]
    public async Task All_Resolved_With_One_Confirmed_Means_Probe()
    {
        var wave = await AddWave();
        await AddAttempt(wave, _upstreamAId);
        await AddAttempt(wave, _upstreamBId);
        await AddAttempt(wave, _consumerId);
        await AddEdge(wave, _consumerId, _upstreamAId, SignalEdgeStatus.Confirmed);
        await AddEdge(wave, _consumerId, _upstreamBId, SignalEdgeStatus.NotNeeded);

        using var reader = Readiness();

        Assert.Equal(ProbeReadiness.Probe, await reader.Read(_consumerId, _organizationId));
    }

    [Fact]
    public async Task All_Resolved_With_None_Confirmed_Means_Nothing_To_Probe()
    {
        var wave = await AddWave();
        await AddAttempt(wave, _upstreamAId);
        await AddAttempt(wave, _upstreamBId);
        await AddAttempt(wave, _consumerId);
        await AddEdge(wave, _consumerId, _upstreamAId, SignalEdgeStatus.NotNeeded);
        await AddEdge(wave, _consumerId, _upstreamBId, SignalEdgeStatus.Unanswerable);

        using var reader = Readiness();

        // Unanswerable releases the consumer rather than holding it: an upstream that will never
        // report must not park an apply forever.
        Assert.Equal(ProbeReadiness.NothingToProbe, await reader.Read(_consumerId, _organizationId));
    }

    [Fact]
    public async Task The_Signal_Object_Is_Copied_Verbatim_Under_The_Variable_Name()
    {
        var wave = await AddWave();
        var set = await AddSignalSet(wave, _upstreamAId, ("vpc_id", VpcSignal, false));
        await AddAttempt(wave, _upstreamAId, set);
        await AddAttempt(wave, _consumerId);
        await AddEdge(wave, _consumerId, _upstreamAId, SignalEdgeStatus.Confirmed);
        await AddParamFromOutput(_consumerId, _upstreamAId, "vpc_id", "network_id");

        using var builder = Builder();
        var json = await builder.BuildInputsJson(wave, _consumerId, _organizationId);

        Assert.NotNull(json);
        var document = JsonNode.Parse(json!)!;

        Assert.Equal("1", document["format_version"]!.GetValue<string>());

        var variables = document["variables"]!.AsObject();
        Assert.True(variables.ContainsKey("network_id"));
        Assert.False(variables.ContainsKey("vpc_id"));

        // The value object is planwalker's, so it must arrive unchanged rather than reassembled.
        var value = variables["network_id"]!.AsObject();
        Assert.Equal("update", value["action"]!.GetValue<string>());
        Assert.Equal("InN0cmluZyI=", value["type"]!.GetValue<string>());
        Assert.Equal("sGNoYW1waW9uLWhlcnJpbmc=", value["before"]!.GetValue<string>());
        Assert.Equal("1AAA", value["after"]!.GetValue<string>());
    }

    [Fact]
    public async Task An_Output_The_Consumer_Does_Not_Read_Is_Left_Out()
    {
        var wave = await AddWave();
        var set = await AddSignalSet(wave, _upstreamAId,
            ("vpc_id", VpcSignal, false),
            ("unread_id", OtherSignal, false));
        await AddAttempt(wave, _upstreamAId, set);
        await AddAttempt(wave, _consumerId);
        await AddEdge(wave, _consumerId, _upstreamAId, SignalEdgeStatus.Confirmed);
        await AddParamFromOutput(_consumerId, _upstreamAId, "vpc_id", "network_id");

        using var builder = Builder();
        var variables = JsonNode.Parse((await builder.BuildInputsJson(wave, _consumerId, _organizationId))!)!
            ["variables"]!.AsObject();

        Assert.Single(variables);
        Assert.True(variables.ContainsKey("network_id"));
    }

    /// <summary>
    /// Only an output-set input can collide. A named-output input records the variable it feeds in
    /// ModuleInputs.Name, and the unique index over (ModuleId, InputKind, Name, OrganizationId)
    /// refuses a second row for the same variable. An output-set input records only the producer,
    /// and every output arrives under its own name, so two producers that both export `region`
    /// collide without either row naming it.
    /// </summary>
    [Fact]
    public async Task Two_Upstreams_Feeding_One_Variable_Produce_One_Entry()
    {
        var wave = await AddWave();

        var setA = await AddSignalSet(wave, _upstreamAId, ("region", VpcSignal, false));
        var setB = await AddSignalSet(wave, _upstreamBId, ("region", OtherSignal, false));

        await AddAttempt(wave, _upstreamAId, setA);
        await AddAttempt(wave, _upstreamBId, setB);
        await AddAttempt(wave, _consumerId);

        await AddEdge(wave, _consumerId, _upstreamAId, SignalEdgeStatus.Confirmed);
        await AddEdge(wave, _consumerId, _upstreamBId, SignalEdgeStatus.Confirmed);

        await AddParamFromOutputSet(_consumerId, _upstreamAId);
        await AddParamFromOutputSet(_consumerId, _upstreamBId);
        await AddVariableSet(_consumerId, "region");

        using var builder = Builder();
        var variables = JsonNode.Parse((await builder.BuildInputsJson(wave, _consumerId, _organizationId))!)!
            ["variables"]!.AsObject();

        // Mirroring the apply: one of the two wins rather than the probe refusing or emitting both.
        Assert.Single(variables);
        Assert.True(variables.ContainsKey("region"));

        // Which one wins is fixed by ordering on the upstream Module, so the same configuration
        // resolves the same way every run. Asserting the count alone would pass even with no
        // collision handling at all, since the later write would simply overwrite the earlier.
        //
        // The expected winner is read back in the database's own ordering: SQL Server compares
        // uniqueidentifier by a different byte order than Guid.CompareTo does.
        await using var db = _fixture.CreateDbContext();
        var firstUpstream = await db.ModuleProbeAttempts
            .Where(p => p.ProbeWaveId == wave && p.ModuleId != _consumerId)
            .OrderBy(p => p.ModuleId)
            .Select(p => p.ModuleId)
            .FirstAsync();

        var expected = firstUpstream == _upstreamAId ? "update" : "create";
        Assert.Equal(expected, variables["region"]!.AsObject()["action"]!.GetValue<string>());
    }

    [Fact]
    public async Task A_Sensitive_Signal_Round_Trips_Through_The_Secret_Store()
    {
        var wave = await AddWave();
        var set = await AddSignalSet(wave, _upstreamAId, ("secret_id", VpcSignal, true));
        await AddAttempt(wave, _upstreamAId, set);
        await AddAttempt(wave, _consumerId);
        await AddEdge(wave, _consumerId, _upstreamAId, SignalEdgeStatus.Confirmed);
        await AddParamFromOutput(_consumerId, _upstreamAId, "secret_id", "token");

        var store = new FakeSignalSecretReader { [RemoteNameFor(set, "secret_id")] = VpcSignal };

        using var builder = Builder(store);
        var variables = JsonNode.Parse((await builder.BuildInputsJson(wave, _consumerId, _organizationId))!)!
            ["variables"]!.AsObject();

        var value = variables["token"]!.AsObject();
        Assert.Equal("sGNoYW1waW9uLWhlcnJpbmc=", value["before"]!.GetValue<string>());
        Assert.Equal("1AAA", value["after"]!.GetValue<string>());
    }

    [Fact]
    public async Task An_Upstream_That_Has_Not_Probed_Yet_Contributes_Nothing()
    {
        var wave = await AddWave();
        await AddAttempt(wave, _upstreamAId);
        await AddAttempt(wave, _consumerId);
        await AddEdge(wave, _consumerId, _upstreamAId, SignalEdgeStatus.Confirmed);
        await AddParamFromOutput(_consumerId, _upstreamAId, "vpc_id", "network_id");

        using var builder = Builder();

        Assert.Null(await builder.BuildInputsJson(wave, _consumerId, _organizationId));
    }

    private ProbeReadinessReader Readiness() => new(_fixture.CreateDbContext());

    private ProbeInputsBuilder Builder(ISignalSecretReader? secretReader = null) =>
        new(
            _fixture.CreateDbContext(),
            new RippleProbeConsumerLookup(_fixture.CreateDbContext()),
            secretReader ?? new FakeSignalSecretReader(),
            NullLogger<ProbeInputsBuilder>.Instance);

    private static string RemoteNameFor(Guid setId, string name) => $"signal--{setId}--{name}";

    private async Task<Guid> AddWave()
    {
        var wave = new ProbeWave
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            OriginModuleId = _upstreamAId,
            OriginModuleJobId = Guid.NewGuid()
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
                    RemoteSecretName = RemoteNameFor(set.Id, name)
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

    private async Task AddParamFromOutput(Guid moduleId, Guid outputModuleId, string outputName, string variableName)
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

    private async Task AddParamFromOutputSet(Guid moduleId, Guid outputModuleId)
    {
        var input = new ModuleParamFromOutputSet
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            OrganizationId = _organizationId,
            Name = $"set-{outputModuleId}",
            OutputModuleId = outputModuleId
        };

        await using var db = _fixture.CreateDbContext();
        db.ModuleInputs.Add(input);
        await db.SaveChangesAsync();

        _inputs.Add(input.Id);
    }

    private async Task AddVariableSet(Guid moduleId, params string[] variableNames)
    {
        var setId = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.VariableSets.Add(new VariableSet
        {
            Id = setId,
            ModuleId = moduleId,
            OrganizationId = _organizationId,
            // Ahead of the fixture's own sets so the latest is unambiguous.
            Timestamp = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
            Checksum = "test",
            Variables = variableNames.Select(n => new Variable
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                VariableSetId = setId,
                Name = n
            }).ToList()
        });
        await db.SaveChangesAsync();

        _variableSets.Add(setId);
    }

    private class FakeSignalSecretReader : Dictionary<string, string>, ISignalSecretReader
    {
        public Task<string?> Read(SecretSignalOutput signal, Guid organizationId) =>
            Task.FromResult(TryGetValue(signal.RemoteSecretName, out var value) ? value : null);
    }
}
