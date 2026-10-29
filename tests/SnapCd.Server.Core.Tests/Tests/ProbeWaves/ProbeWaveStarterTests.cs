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
using SnapCd.Server.Core.Services.Nonsecured.RippleProbes;
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

    private Guid _consumerAppliedJobId;
    private readonly List<Guid> _originJobs = [];

    public async Task InitializeAsync()
    {
        _organizationId = _fixture.Organizations["0"].Id;
        _originModuleId = _fixture.Modules["0001"].Id;
        _consumerModuleId = _fixture.Modules["0000"].Id;

        // Only a Module with state to plan against is enrolled, so the consumer needs an apply
        // behind it for these tests to be about edge creation rather than about that filter.
        _consumerAppliedJobId = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(new ModuleJob
        {
            Id = _consumerAppliedJobId,
            OrganizationId = _organizationId,
            ModuleId = _consumerModuleId,
            JobType = "Apply",
            Status = ExecutionStatus.Completed,
            ActualStateHeadline = ActualStateHeadline.Applied,
            IsCurrent = false,
            CreatedDateTime = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();

        var waveIds = await db.ProbeWaves
            .Where(w => w.OriginModuleJob.ModuleId == _originModuleId ||
                        w.OriginModuleJob.ModuleId == _consumerModuleId)
            .Select(w => w.Id)
            .ToListAsync();

        await db.SignalEdges.Where(e => waveIds.Contains(e.ProbeWaveId)).ExecuteDeleteAsync();
        await db.ModuleProbeAttempts.Where(p => waveIds.Contains(p.ProbeWaveId)).ExecuteDeleteAsync();
        var setIds = await db.SignalOutputSets.Where(x => waveIds.Contains(x.ProbeWaveId)).Select(x => x.Id).ToListAsync();
        await db.SignalOutputs.Where(o => setIds.Contains(o.SignalOutputSetId)).ExecuteDeleteAsync();
        await db.SignalOutputSets.Where(x => setIds.Contains(x.Id)).ExecuteDeleteAsync();
        await db.ProbeWaves.Where(w => waveIds.Contains(w.Id)).ExecuteDeleteAsync();

        if (_inputs.Count > 0)
            await db.ModuleInputs.Where(i => _inputs.Contains(i.Id)).ExecuteDeleteAsync();

        await db.ModuleJobs.Where(j => j.Id == _consumerAppliedJobId || _originJobs.Contains(j.Id)).ExecuteDeleteAsync();
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
        var edges = await db.SignalEdges
            .AsNoTracking()
            .Where(e => e.ProbeWaveId == waveId!.Value)
            .ToListAsync();

        Assert.NotEmpty(edges);
        Assert.All(edges, e => Assert.Equal(SignalEdgeStatus.Confirmed, e.Status));
    }

    /// <summary>
    /// No edge points at the origin: it answers from its own plan rather than being probed, so it
    /// is never a consumer in its own wave. Every edge the wave starts with comes out of it.
    /// </summary>
    [Fact]
    public async Task No_Edge_Points_At_The_Origin()
    {
        await AddParamFromOutput(_consumerModuleId, "vpc_id", "network_id");

        var waveId = await Start(["vpc_id"]);

        await using var db = _fixture.CreateDbContext();
        var edges = await db.SignalEdges
            .AsNoTracking()
            .Where(e => e.ProbeWaveId == waveId!.Value)
            .ToListAsync();

        Assert.NotEmpty(edges);
        Assert.All(edges, e => Assert.Equal(_originModuleId, e.UpstreamModuleId));
        Assert.DoesNotContain(_originModuleId, edges.Select(e => e.ConsumerModuleId));
    }

    /// <summary>
    /// Only the Modules the change reaches are enrolled. A Module that reads a different output of
    /// the same producer is reachable and uninvolved, and writing it in order to dismiss it would
    /// fill a wave with rows nobody acts on.
    /// </summary>
    /// <summary>
    /// A probe plans the consumer with a predicted value. A Module that has never reached a
    /// terminal state has no state and no outputs, so that plan answers a different question.
    /// </summary>
    [Fact]
    public async Task A_Consumer_That_Has_Never_Applied_Gets_No_Edge()
    {
        var neverApplied = await AddModuleWithoutOutputs();
        await AddParamFromOutput(neverApplied, "vpc_id", "network_id");

        var waveId = await Start(["vpc_id"]);

        if (waveId is null) return;

        await using var db = _fixture.CreateDbContext();
        var moduleIds = await db.SignalEdges
            .AsNoTracking()
            .Where(e => e.ProbeWaveId == waveId.Value)
            .Select(e => e.ConsumerModuleId)
            .ToListAsync();

        Assert.DoesNotContain(neverApplied, moduleIds);
    }

    /// <summary>
    /// A destroyed Module has no outputs left for a consumer to plan against, and asking it for an
    /// Apply would undo the destroy. The filter reads the state at creation, so it catches the
    /// Module destroyed before the wave rather than one destroyed while it is open.
    /// </summary>
    [Fact]
    public async Task A_Destroyed_Consumer_Gets_No_Edge()
    {
        await AddParamFromOutput(_consumerModuleId, "vpc_id", "network_id");

        var destroyedJobId = Guid.NewGuid();
        _originJobs.Add(destroyedJobId);

        await using (var seed = _fixture.CreateDbContext())
        {
            // A later job than the Applied one the fixture seeds, so the most recent finished job
            // for this Module is the destroy.
            seed.ModuleJobs.Add(new ModuleJob
            {
                Id = destroyedJobId,
                OrganizationId = _organizationId,
                ModuleId = _consumerModuleId,
                JobType = "Destroy",
                Status = ExecutionStatus.Completed,
                ActualStateHeadline = ActualStateHeadline.Destroyed,
                TimestampStart = DateTime.UtcNow.AddMinutes(-5),
                TimestampEnd = DateTime.UtcNow,
                IsCurrent = false,
                CreatedDateTime = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        var waveId = await Start(["vpc_id"]);

        if (waveId is null) return;

        await using var db = _fixture.CreateDbContext();
        var moduleIds = await db.SignalEdges
            .AsNoTracking()
            .Where(e => e.ProbeWaveId == waveId.Value)
            .Select(e => e.ConsumerModuleId)
            .ToListAsync();

        Assert.DoesNotContain(_consumerModuleId, moduleIds);
    }

    /// <summary>
    /// Destroyed then applied again is applied. The check reads the most recent finished job, so a
    /// Module that came back is enrolled like any other.
    /// </summary>
    [Fact]
    public async Task A_Consumer_Reapplied_After_A_Destroy_Gets_An_Edge()
    {
        await AddParamFromOutput(_consumerModuleId, "vpc_id", "network_id");

        var destroyedJobId = Guid.NewGuid();
        var reappliedJobId = Guid.NewGuid();
        _originJobs.Add(destroyedJobId);
        _originJobs.Add(reappliedJobId);

        await using (var seed = _fixture.CreateDbContext())
        {
            seed.ModuleJobs.Add(new ModuleJob
            {
                Id = destroyedJobId,
                OrganizationId = _organizationId,
                ModuleId = _consumerModuleId,
                JobType = "Destroy",
                Status = ExecutionStatus.Completed,
                ActualStateHeadline = ActualStateHeadline.Destroyed,
                TimestampStart = DateTime.UtcNow.AddMinutes(-20),
                TimestampEnd = DateTime.UtcNow.AddMinutes(-10),
                IsCurrent = false,
                CreatedDateTime = DateTime.UtcNow
            });
            // Saved separately: JobNumber is an identity column, so insert order is what makes
            // the re-apply the most recent finished job.
            await seed.SaveChangesAsync();

            seed.ModuleJobs.Add(new ModuleJob
            {
                Id = reappliedJobId,
                OrganizationId = _organizationId,
                ModuleId = _consumerModuleId,
                JobType = "Apply",
                Status = ExecutionStatus.Completed,
                ActualStateHeadline = ActualStateHeadline.Applied,
                TimestampStart = DateTime.UtcNow.AddMinutes(-5),
                TimestampEnd = DateTime.UtcNow,
                IsCurrent = false,
                CreatedDateTime = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        var waveId = await Start(["vpc_id"]);

        Assert.NotNull(waveId);

        await using var db = _fixture.CreateDbContext();
        var moduleIds = await db.SignalEdges
            .AsNoTracking()
            .Where(e => e.ProbeWaveId == waveId!.Value)
            .Select(e => e.ConsumerModuleId)
            .ToListAsync();

        Assert.Contains(_consumerModuleId, moduleIds);
    }

    [Fact]
    public async Task A_Consumer_Of_An_Unchanged_Output_Gets_No_Edge()
    {
        await AddParamFromOutput(_consumerModuleId, "vpc_id", "network_id");
        await AddParamFromOutput(_fixture.Modules["0002"].Id, "untouched", "from_untouched");

        var waveId = await Start(["vpc_id"]);

        await using var db = _fixture.CreateDbContext();
        var moduleIds = await db.SignalEdges
            .AsNoTracking()
            .Where(e => e.ProbeWaveId == waveId!.Value)
            .Select(e => e.ConsumerModuleId)
            .ToListAsync();

        Assert.Contains(_consumerModuleId, moduleIds);
        Assert.DoesNotContain(_fixture.Modules["0002"].Id, moduleIds);
    }

    /// <summary>
    /// A Module two hops down is not enrolled at creation, because whether the change reaches it
    /// depends on what the Module above it turns out to change, which no plan has said yet.
    /// </summary>
    [Fact]
    public async Task A_Second_Hop_Gets_No_Edge_At_Creation()
    {
        await AddParamFromOutput(_consumerModuleId, "vpc_id", "network_id");

        // Reads an output of the consumer rather than of the origin.
        await AddParamFromOutput(
            _fixture.Modules["0002"].Id, "downstream_id", "from_consumer",
            outputModuleId: _consumerModuleId);

        var waveId = await Start(["vpc_id"]);

        await using var db = _fixture.CreateDbContext();
        var moduleIds = await db.SignalEdges
            .AsNoTracking()
            .Where(e => e.ProbeWaveId == waveId!.Value)
            .Select(e => e.ConsumerModuleId)
            .ToListAsync();

        Assert.Single(moduleIds);
        Assert.DoesNotContain(_fixture.Modules["0002"].Id, moduleIds);
    }

    /// <summary>
    /// Participants are the Modules downstream of the origin, not the ones it reads from. The
    /// closure holds both directions, and taking the wrong one builds a wave whose participants
    /// can never receive a signal.
    /// </summary>
    [Fact]
    public async Task A_Module_The_Origin_Reads_From_Gets_No_Edge()
    {
        await AddParamFromOutput(_consumerModuleId, "vpc_id", "network_id");

        // The origin reads from a third Module, which puts that Module upstream of the origin and
        // so outside the wave.
        var upstreamOfOrigin = _fixture.Modules["0010"].Id;
        await AddParamFromOutput(_originModuleId, "db_host", "database_host", outputModuleId: upstreamOfOrigin);

        var waveId = await Start(["vpc_id"]);

        await using var db = _fixture.CreateDbContext();
        var moduleIds = await db.SignalEdges
            .AsNoTracking()
            .Where(e => e.ProbeWaveId == waveId!.Value)
            .Select(e => e.ConsumerModuleId)
            .ToListAsync();

        Assert.Contains(_consumerModuleId, moduleIds);
        Assert.DoesNotContain(upstreamOfOrigin, moduleIds);
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
        var origin = await db.ModuleProbeAttempts
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

        var jobId = await AddOriginJob();
        var waveId = await Start(["vpc_id"], jobId);

        await using var db = _fixture.CreateDbContext();
        var wave = await db.ProbeWaves
            .AsNoTracking()
            .Include(w => w.OriginModuleJob)
            .FirstAsync(w => w.Id == waveId!.Value);

        Assert.Equal(_originModuleId, wave.OriginModuleJob.ModuleId);
        Assert.Equal(jobId, wave.OriginModuleJobId);
    }


    /// <summary>
    /// The origin job whose approval the wave informs, and which it is constrained to. A wave
    /// cannot be written without it.
    /// </summary>
    private async Task<Guid> AddOriginJob()
    {
        var jobId = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.ModuleJobs.Add(new ModuleJob
        {
            Id = jobId,
            OrganizationId = _organizationId,
            ModuleId = _originModuleId,
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

    private async Task<Guid?> Start(
        string[] changedOutputNames,
        Guid? originJobId = null,
        string? outputSignalsJson = null)
    {
        // One context across the three, as a scope gives them: a lookup on its own connection
        // would not see what the starter has written and not yet committed.
        await using var dbContext = _fixture.CreateDbContext();

        using var starter = new ProbeWaveStarter(
            dbContext,
            new RippleProbeConsumerLookup(dbContext),
            new ProbeSignalRecorder(
                dbContext,
                Mock.Of<ISignalSecretWriter>(),
                Mock.Of<IPublishEndpoint>(),
                NullLogger<ProbeSignalRecorder>.Instance),
            Mock.Of<IPublishEndpoint>(),
            NullLogger<ProbeWaveStarter>.Instance);

        return await starter.Start(
            _originModuleId,
            _organizationId,
            originJobId ?? await AddOriginJob(),
            changedOutputNames,
            outputSignalsJson);
    }

    private sealed class FixtureDbContextFactory(Fixture fixture) : IDbContextFactory<SnapCdDbContext>
    {
        public SnapCdDbContext CreateDbContext() => fixture.CreateDbContext();
    }

    /// <summary>
    /// A Module in the origin's Namespace with no output set, which is how a Module that has never
    /// applied looks. The fixture's Modules in this Namespace all have one.
    /// </summary>
    private async Task<Guid> AddModuleWithoutOutputs()
    {
        var module = new Module
        {
            Id = Guid.NewGuid(),
            Name = $"never-applied-{Guid.NewGuid():N}",
            NamespaceId = _fixture.Modules["0000"].NamespaceId,
            RunnerId = _fixture.Modules["0000"].RunnerId,
            OrganizationId = _organizationId,
            SourceUrl = "https://github.com/test/never-applied",
            SourceRevision = "main",
            SourceSubdirectory = "terraform",
            CreatedDateTime = DateTime.UtcNow
        };

        await using var db = _fixture.CreateDbContext();
        db.Modules.Add(module);
        await db.SaveChangesAsync();

        return module.Id;
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
