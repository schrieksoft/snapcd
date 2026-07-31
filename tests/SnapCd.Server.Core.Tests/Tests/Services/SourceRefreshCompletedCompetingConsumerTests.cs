// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Moq;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Server.Core.Consumers.System.Competing;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.StateMachine.Gatekeeping;
using SnapCd.Server.Core.Tests.Infrastructure;
using Module = SnapCd.Server.Core.Entities.Definition.Module;
using Namespace = SnapCd.Server.Core.Entities.Definition.Namespace;

namespace SnapCd.Server.Core.Tests.Tests.Services;

[Collection("NewRoleBasedSharedFixture")]
public class SourceRefreshCompletedCompetingConsumerTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private string _sourceUrl = null!;
    private Namespace _namespace = null!;
    private Runner _runner = null!;
    private Module _filterOnModule = null!;
    private Module _filterOffModule = null!;
    private Module _notificationOnlyModule = null!;

    public SourceRefreshCompletedCompetingConsumerTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _sourceUrl = $"https://github.com/test/trigger-paths-{Guid.NewGuid():N}";

        await using var dbContext = _fixture.CreateDbContext();
        _namespace = dbContext.Namespaces.First();
        _runner = dbContext.Runners.First(r => r.OrganizationId == _namespace.OrganizationId);

        _filterOnModule = CreateModule("filter-on", filterEnabled: true, subdirectory: "modules/app-a");
        _filterOnModule.AdditionalTriggerPaths.Add(new ModuleAdditionalTriggerPath
        {
            Id = Guid.NewGuid(),
            OrganizationId = _namespace.OrganizationId,
            ModuleId = _filterOnModule.Id,
            Path = "shared/scripts",
            CreatedDateTime = DateTime.UtcNow
        });

        _filterOffModule = CreateModule("filter-off", filterEnabled: false, subdirectory: "modules/app-b");

        _notificationOnlyModule = CreateModule("notif-only", filterEnabled: true, subdirectory: "modules/app-c");
        _notificationOnlyModule.TriggerOnSourceChanged = false;
        _notificationOnlyModule.TriggerOnSourceChangedNotification = true;

        dbContext.Modules.AddRange(_filterOnModule, _filterOffModule, _notificationOnlyModule);
        await dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await using var dbContext = _fixture.CreateDbContext();
        dbContext.Modules.RemoveRange(dbContext.Modules.Where(m => m.SourceUrl == _sourceUrl));
        await dbContext.SaveChangesAsync();
    }

    private Module CreateModule(string name, bool filterEnabled, string subdirectory)
    {
        var module = new Module
        {
            Id = Guid.NewGuid(),
            Name = $"{name}-{Guid.NewGuid():N}"[..40],
            NamespaceId = _namespace.Id,
            RunnerId = _runner.Id,
            OrganizationId = _namespace.OrganizationId,
            SourceUrl = _sourceUrl,
            SourceRevision = "main",
            SourceSubdirectory = subdirectory,
            TriggerOnSourceChanged = true,
            TriggerPathFilterEnabled = filterEnabled,
            CreatedDateTime = DateTime.UtcNow
        };
        module.ModuleSaga = new ModuleSaga
        {
            CorrelationId = module.Id,
            OrganizationId = module.OrganizationId,
            RowVersion = [],
            CurrentState = nameof(ModuleStateMachine.Gatekeeping)
        };
        module.ModuleModifiedSaga = new ModuleModifiedSaga
        {
            CorrelationId = module.Id,
            OrganizationId = module.OrganizationId,
            RowVersion = [],
            CurrentState = nameof(ModuleModifiedStateMachine.Idle)
        };
        return module;
    }

    private async Task<List<GatekeepingJobRequested>> Consume(string definitiveRevision, List<PathHash>? pathHashes, List<ModuleClosure>? moduleClosures = null, bool triggeredByNotification = false)
    {
        var published = new List<GatekeepingJobRequested>();

        var contextMock = new Mock<ConsumeContext<SourceRefreshCompleted>>();
        contextMock.SetupGet(c => c.Message).Returns(new SourceRefreshCompleted
        {
            SourceUrl = _sourceUrl,
            SourceRevision = "main",
            DefinitiveRevision = definitiveRevision,
            PathHashes = pathHashes,
            ModuleClosures = moduleClosures,
            TriggeredByNotification = triggeredByNotification
        });
        contextMock
            .Setup(c => c.Publish(It.IsAny<GatekeepingJobRequested>(), It.IsAny<IPipe<PublishContext<GatekeepingJobRequested>>>(), It.IsAny<CancellationToken>()))
            .Callback<GatekeepingJobRequested, IPipe<PublishContext<GatekeepingJobRequested>>, CancellationToken>((message, _, _) => published.Add(message))
            .Returns(Task.CompletedTask);

        await using var dbContext = _fixture.CreateDbContext();
        var consumer = new SourceRefreshCompletedCompetingConsumer(dbContext);
        await consumer.Consume(contextMock.Object);

        return published;
    }

    private async Task SetSagaState(Module module, string? desiredClosureHash, string? desiredDefinitiveRevision)
    {
        await using var dbContext = _fixture.CreateDbContext();
        var saga = dbContext.ModuleSagas.Single(s => s.CorrelationId == module.Id);
        saga.DesiredClosureHash = desiredClosureHash;
        saga.DesiredDefinitiveRevision = desiredDefinitiveRevision;
        await dbContext.SaveChangesAsync();
    }

    private static List<PathHash> Report(params (string Path, string TreeHash)[] hashes)
    {
        return hashes.Select(h => new PathHash { Path = h.Path, TreeHash = h.TreeHash }).ToList();
    }

    [Fact]
    public async Task PathAware_Refresh_Triggers_Only_Modules_Whose_Closure_Moved()
    {
        var report = Report(("modules/app-a", "hash-a"), ("shared/scripts", "hash-s"), ("modules/app-b", "hash-b"));
        var reported = report.ToDictionary(p => p.Path, p => p.TreeHash);

        // filter-on module: stored hash equals the composition over its watched set -> suppressed
        var unchangedComposition = TriggerPathClosure.Compose(new[] { "modules/app-a", "shared/scripts" }, reported);
        await SetSagaState(_filterOnModule, unchangedComposition, "old-sha");
        // filter-off module: revision moved -> legacy trigger
        await SetSagaState(_filterOffModule, null, "old-sha");

        var published = await Consume("new-sha", report);

        Assert.DoesNotContain(published, p => p.ModuleId == _filterOnModule.Id);
        var filterOffTrigger = Assert.Single(published, p => p.ModuleId == _filterOffModule.Id);
        Assert.Null(filterOffTrigger.DesiredClosureHash);
        Assert.Equal("new-sha", filterOffTrigger.DefinitiveRevision);
    }

    [Fact]
    public async Task PathAware_Refresh_Triggers_When_A_Watched_Hash_Moved()
    {
        var oldReport = Report(("modules/app-a", "hash-a"), ("shared/scripts", "hash-s"));
        var oldComposition = TriggerPathClosure.Compose(new[] { "modules/app-a", "shared/scripts" }, oldReport.ToDictionary(p => p.Path, p => p.TreeHash));
        await SetSagaState(_filterOnModule, oldComposition, "old-sha");
        await SetSagaState(_filterOffModule, null, "new-sha");

        var newReport = Report(("modules/app-a", "hash-a"), ("shared/scripts", "hash-s2"), ("modules/app-b", "hash-b"));
        var published = await Consume("new-sha", newReport);

        var trigger = Assert.Single(published);
        Assert.Equal(_filterOnModule.Id, trigger.ModuleId);
        Assert.Equal(
            TriggerPathClosure.Compose(new[] { "modules/app-a", "shared/scripts" }, newReport.ToDictionary(p => p.Path, p => p.TreeHash)),
            trigger.DesiredClosureHash);
    }

    [Fact]
    public async Task Null_Stored_Hash_Fails_Open()
    {
        await SetSagaState(_filterOnModule, null, "new-sha");
        await SetSagaState(_filterOffModule, null, "new-sha");

        var published = await Consume("new-sha", Report(("modules/app-a", "hash-a"), ("shared/scripts", "hash-s"), ("modules/app-b", "hash-b")));

        var trigger = Assert.Single(published);
        Assert.Equal(_filterOnModule.Id, trigger.ModuleId);
        Assert.NotNull(trigger.DesiredClosureHash);
    }

    [Fact]
    public async Task Legacy_Refresh_Without_PathHashes_Uses_Revision_Comparison()
    {
        await SetSagaState(_filterOnModule, null, "old-sha");
        await SetSagaState(_filterOffModule, null, "old-sha");

        var published = await Consume("new-sha", pathHashes: null);

        Assert.Equal(2, published.Count);
        Assert.All(published, p => Assert.Null(p.DesiredClosureHash));
    }

    [Fact]
    public async Task Notification_Only_Module_Is_Ignored_By_Scheduled_Refreshes()
    {
        // Null stored hash would fail open if the module were eligible — proving it was never evaluated.
        await SetSagaState(_notificationOnlyModule, null, null);
        await SetSagaState(_filterOnModule, null, "new-sha");
        await SetSagaState(_filterOffModule, null, "new-sha");

        var report = Report(("modules/app-a", "hash-a"), ("shared/scripts", "hash-s"), ("modules/app-b", "hash-b"), ("modules/app-c", "hash-c"));
        var published = await Consume("new-sha", report, triggeredByNotification: false);

        Assert.DoesNotContain(published, p => p.ModuleId == _notificationOnlyModule.Id);
    }

    [Fact]
    public async Task Notification_Refresh_Evaluates_Notification_Only_Module_By_Hash()
    {
        var report = Report(("modules/app-a", "hash-a"), ("shared/scripts", "hash-s"), ("modules/app-b", "hash-b"), ("modules/app-c", "hash-c"));
        var reported = report.ToDictionary(p => p.Path, p => p.TreeHash);

        // Keep the always-on modules quiet so only the notification-only module's decision is visible.
        await SetSagaState(_filterOnModule, TriggerPathClosure.Compose(new[] { "modules/app-a", "shared/scripts" }, reported), "new-sha");
        await SetSagaState(_filterOffModule, null, "new-sha");
        await SetSagaState(_notificationOnlyModule, TriggerPathClosure.Compose(new[] { "modules/app-c" }, reported), "old-sha");

        var unchanged = await Consume("new-sha", report, triggeredByNotification: true);
        Assert.Empty(unchanged);

        var movedReport = Report(("modules/app-a", "hash-a"), ("shared/scripts", "hash-s"), ("modules/app-b", "hash-b"), ("modules/app-c", "hash-c2"));
        var published = await Consume("new-sha", movedReport, triggeredByNotification: true);

        var trigger = Assert.Single(published);
        Assert.Equal(_notificationOnlyModule.Id, trigger.ModuleId);
    }

    [Fact]
    public async Task Discovered_Reference_Widens_The_Closure_And_Its_Change_Triggers()
    {
        var closures = new List<ModuleClosure>
        {
            new() { RootPath = "modules/app-a", ReferencedPaths = { "shared/network" } }
        };
        var oldReport = Report(("modules/app-a", "hash-a"), ("shared/scripts", "hash-s"), ("shared/network", "hash-n"), ("modules/app-b", "hash-b"));
        var oldComposition = TriggerPathClosure.Compose(
            new[] { "modules/app-a", "shared/scripts", "shared/network" },
            oldReport.ToDictionary(p => p.Path, p => p.TreeHash));

        await SetSagaState(_filterOnModule, oldComposition, "old-sha");
        await SetSagaState(_filterOffModule, null, "new-sha");

        // Only the discovered directory's hash moves — neither the subdirectory nor any declared path changed.
        var newReport = Report(("modules/app-a", "hash-a"), ("shared/scripts", "hash-s"), ("shared/network", "hash-n2"), ("modules/app-b", "hash-b"));
        var published = await Consume("new-sha", newReport, closures);

        var trigger = Assert.Single(published);
        Assert.Equal(_filterOnModule.Id, trigger.ModuleId);
    }

    [Fact]
    public async Task Identical_Hashes_Under_New_Head_Do_Not_Trigger_Filtered_Module()
    {
        var report = Report(("modules/app-a", "hash-a"), ("shared/scripts", "hash-s"), ("modules/app-b", "hash-b"));
        var composition = TriggerPathClosure.Compose(new[] { "modules/app-a", "shared/scripts" }, report.ToDictionary(p => p.Path, p => p.TreeHash));

        // The force-push / rebase property: head SHA moved, no watched tree hash moved.
        await SetSagaState(_filterOnModule, composition, "old-sha");
        await SetSagaState(_filterOffModule, null, "rewritten-sha");

        var published = await Consume("rewritten-sha", report);

        Assert.DoesNotContain(published, p => p.ModuleId == _filterOnModule.Id);
    }
}
