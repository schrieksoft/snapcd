// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.StateMachine.Gatekeeping;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Deletes;

/// <summary>
/// A signal edge names two Modules and restricts on both, so it cannot cascade: SQL Server refuses
/// two cascade paths into one table. Without a sweep, a wave the Module once appeared in blocks
/// the delete for good, and a Module outlives a wave by a long way.
///
/// Each delete path needs its own test, because they do not share code: a namespace delete
/// cascades its Modules rather than deleting them through ModuleRepository, and a stack delete
/// cascades its namespaces.
///
/// Every test puts the Module under test on both ends of an edge - consumer of one, upstream of
/// another - because the two foreign keys are separate constraints and a sweep that cleared only
/// one side would pass a test that used only one.
///
/// These build their own stack, namespace and modules rather than using the fixture's, because the
/// test deletes them and the fixture is shared across the collection.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class SignalEdgeBlocksDeleteTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _organizationId;
    private Guid _runnerId;
    private readonly List<Guid> _stacks = [];
    private readonly List<Guid> _waves = [];

    public SignalEdgeBlocksDeleteTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _organizationId = _fixture.Organizations["0"].Id;
        _runnerId = _fixture.Runners["0"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();

        var namespaceIds = await db.Namespaces
            .Where(n => _stacks.Contains(n.StackId)).Select(n => n.Id).ToListAsync();
        var moduleIds = await db.Modules
            .Where(m => namespaceIds.Contains(m.NamespaceId)).Select(m => m.Id).ToListAsync();

        await db.SignalEdges.Where(e => _waves.Contains(e.ProbeWaveId)).ExecuteDeleteAsync();
        await db.ModuleProbeAttempts.Where(a => _waves.Contains(a.ProbeWaveId)).ExecuteDeleteAsync();
        await db.ProbeWaves.Where(w => _waves.Contains(w.Id)).ExecuteDeleteAsync();

        await db.Modules.Where(m => moduleIds.Contains(m.Id)).ExecuteDeleteAsync();
        await db.Set<ModuleSaga>().Where(s => moduleIds.Contains(s.CorrelationId)).ExecuteDeleteAsync();

        await db.Namespaces.Where(n => namespaceIds.Contains(n.Id)).ExecuteDeleteAsync();
        await db.Stacks.Where(s => _stacks.Contains(s.Id)).ExecuteDeleteAsync();
    }

    [Fact]
    public async Task A_Module_With_Edges_On_Both_Sides_Deletes()
    {
        var h = await SeedHierarchy();
        await SeedWaveWithEdges(h);

        await ModuleRepo().ExecuteDelete(h.MiddleId, _organizationId);

        await AssertModuleAndSagasGone(h.MiddleId);

        await using var db = _fixture.CreateDbContext();
        Assert.Empty(await db.SignalEdges.AsNoTracking()
            .Where(e => e.ConsumerModuleId == h.MiddleId || e.UpstreamModuleId == h.MiddleId)
            .ToListAsync());
    }

    /// <summary>
    /// The attempt row records what the Module did in the wave, and restricts on the Module for
    /// the same reason, so it blocks the delete on its own account.
    /// </summary>
    [Fact]
    public async Task A_Module_That_Already_Probed_Deletes()
    {
        var h = await SeedHierarchy();
        var waveId = await SeedWaveWithEdges(h);
        await SeedAttempt(waveId, h.MiddleId);

        await ModuleRepo().ExecuteDelete(h.MiddleId, _organizationId);

        await AssertModuleAndSagasGone(h.MiddleId);

        await using var db = _fixture.CreateDbContext();
        Assert.Empty(await db.ModuleProbeAttempts.AsNoTracking()
            .Where(a => a.ModuleId == h.MiddleId).ToListAsync());
    }

    /// <summary>
    /// Deleting an upstream takes the edge with it rather than marking it. An edge is the
    /// relationship to that Module, so nothing is left for a status to describe: a consumer with
    /// no edges left has simply left the wave, which is what an absent edge means to every reader
    /// of it.
    /// </summary>
    [Fact]
    public async Task Deleting_An_Upstream_Takes_The_Edge_That_Waited_On_It()
    {
        var h = await SeedHierarchy();
        var waveId = await SeedWaveWithEdges(h);

        // The downstream Module waits on the middle one, still unresolved.
        await ModuleRepo().ExecuteDelete(h.MiddleId, _organizationId);

        await using var db = _fixture.CreateDbContext();
        var downstreamEdges = await db.SignalEdges.AsNoTracking()
            .Where(e => e.ProbeWaveId == waveId && e.ConsumerModuleId == h.DownstreamId)
            .ToListAsync();

        Assert.Empty(downstreamEdges);
    }

    /// <summary>
    /// A namespace delete cascades its Modules rather than going through ModuleRepository, so the
    /// sweep has to exist there too.
    /// </summary>
    [Fact]
    public async Task A_Namespace_Holding_Modules_With_Edges_Deletes()
    {
        var h = await SeedHierarchy();
        await SeedWaveWithEdges(h);

        await NamespaceRepo().ExecuteDelete(h.NamespaceId, _organizationId);

        await AssertModuleAndSagasGone(h.UpstreamId);
        await AssertModuleAndSagasGone(h.MiddleId);
        await AssertModuleAndSagasGone(h.DownstreamId);

        await using var db = _fixture.CreateDbContext();
        Assert.Null(await db.Namespaces.AsNoTracking().FirstOrDefaultAsync(n => n.Id == h.NamespaceId));
    }

    /// <summary>A stack delete cascades namespaces, so it reaches the same constraint again.</summary>
    [Fact]
    public async Task A_Stack_Holding_Modules_With_Edges_Deletes()
    {
        var h = await SeedHierarchy();
        await SeedWaveWithEdges(h);

        await StackRepo().ExecuteDelete(h.StackId, _organizationId);

        await AssertModuleAndSagasGone(h.MiddleId);

        await using var db = _fixture.CreateDbContext();
        Assert.Null(await db.Stacks.AsNoTracking().FirstOrDefaultAsync(s => s.Id == h.StackId));
    }

    /// <summary>
    /// The wave itself outlives the Modules, and its own rows are gone. A wave row left behind
    /// with no edges would read as an empty wave rather than a deleted one.
    /// </summary>
    [Fact]
    public async Task A_Stack_Delete_Leaves_No_Edges_Behind()
    {
        var h = await SeedHierarchy();
        var waveId = await SeedWaveWithEdges(h);
        await SeedAttempt(waveId, h.MiddleId);

        await StackRepo().ExecuteDelete(h.StackId, _organizationId);

        await using var db = _fixture.CreateDbContext();
        Assert.Empty(await db.SignalEdges.AsNoTracking().Where(e => e.ProbeWaveId == waveId).ToListAsync());
        Assert.Empty(await db.ModuleProbeAttempts.AsNoTracking().Where(a => a.ProbeWaveId == waveId).ToListAsync());
    }

    private async Task AssertModuleAndSagasGone(Guid moduleId)
    {
        await using var db = _fixture.CreateDbContext();

        Assert.Null(await db.Modules.AsNoTracking().FirstOrDefaultAsync(m => m.Id == moduleId));
        Assert.Null(await db.Set<ModuleSaga>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.CorrelationId == moduleId));
    }

    /// <summary>
    /// A wave over the three Modules: upstream feeds middle, middle feeds downstream. The middle
    /// one is therefore a consumer on one edge and an upstream on another, which is what puts both
    /// foreign keys in the way of deleting it.
    /// </summary>
    private async Task<Guid> SeedWaveWithEdges(Hierarchy h)
    {
        var wave = new ProbeWave
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            OriginModuleId = h.UpstreamId,
            OriginModuleJobId = Guid.NewGuid()
        };

        _waves.Add(wave.Id);

        await using var db = _fixture.CreateDbContext();
        db.ProbeWaves.Add(wave);

        db.SignalEdges.Add(new SignalEdge
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ProbeWaveId = wave.Id,
            ConsumerModuleId = h.MiddleId,
            UpstreamModuleId = h.UpstreamId,
            Status = SignalEdgeStatus.Confirmed
        });

        db.SignalEdges.Add(new SignalEdge
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ProbeWaveId = wave.Id,
            ConsumerModuleId = h.DownstreamId,
            UpstreamModuleId = h.MiddleId,
            Status = SignalEdgeStatus.Potential
        });

        await db.SaveChangesAsync();
        return wave.Id;
    }

    private async Task SeedAttempt(Guid waveId, Guid moduleId)
    {
        await using var db = _fixture.CreateDbContext();
        db.ModuleProbeAttempts.Add(new ModuleProbeAttempt
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ProbeWaveId = waveId,
            ModuleId = moduleId,
            ProbeModuleJobId = Guid.NewGuid()
        });
        await db.SaveChangesAsync();
    }

    private record Hierarchy(Guid StackId, Guid NamespaceId, Guid UpstreamId, Guid MiddleId, Guid DownstreamId);

    private async Task<Hierarchy> SeedHierarchy()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var stack = new Stack
        {
            Id = Guid.NewGuid(), Name = $"edge-stack-{suffix}",
            OrganizationId = _organizationId, CreatedDateTime = DateTime.UtcNow
        };
        _stacks.Add(stack.Id);

        var ns = new Namespace
        {
            Id = Guid.NewGuid(), Name = $"edge-ns-{suffix}", StackId = stack.Id,
            OrganizationId = _organizationId, CreatedDateTime = DateTime.UtcNow
        };

        var upstream = NewModule($"edge-up-{suffix}", ns.Id);
        var middle = NewModule($"edge-mid-{suffix}", ns.Id);
        var downstream = NewModule($"edge-down-{suffix}", ns.Id);

        await using var db = _fixture.CreateDbContext();
        db.Stacks.Add(stack);
        db.Namespaces.Add(ns);
        db.Modules.AddRange(upstream, middle, downstream);
        await db.SaveChangesAsync();

        return new Hierarchy(stack.Id, ns.Id, upstream.Id, middle.Id, downstream.Id);
    }

    private Module NewModule(string name, Guid namespaceId)
    {
        var module = new Module
        {
            Id = Guid.NewGuid(),
            Name = name,
            NamespaceId = namespaceId,
            RunnerId = _runnerId,
            OrganizationId = _organizationId,
            SourceUrl = "https://example.invalid/repo.git",
            SourceRevision = "main",
            SourceSubdirectory = "",
            SourceType = SourceType.Git,
            SourceRevisionType = SourceRevisionType.Default,
            CreatedDateTime = DateTime.UtcNow
        };

        module.ModuleSaga = new ModuleSaga
        {
            CorrelationId = module.Id,
            OrganizationId = _organizationId,
            RowVersion = [],
            CurrentState = nameof(ModuleStateMachine.Gatekeeping),
            DesiredStateHeadline = DesiredStateHeadline.Applied
        };

        return module;
    }

    private ModuleRepository ModuleRepo() =>
        new(_fixture.CreateDbContext(), Principal(), _fixture.CreateMockBus(), _fixture.CreateModuleSettings());

    private NamespaceRepository NamespaceRepo() =>
        new(_fixture.CreateDbContext(), Principal(), _fixture.CreateMockBus(), _fixture.CreateNamespaceSettings());

    private StackRepository StackRepo() =>
        new(_fixture.CreateDbContext(), Principal(), _fixture.CreateMockBus(),
            Options.Create(new StackRepositorySettings()));

    private IPrincipalProvider Principal() =>
        _fixture.CreatePrincipalProvider(Guid.Empty, PrincipalDiscriminator.User, _organizationId);
}
