// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Server.Core.Consumers.System.Competing;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Utils;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// A Module with path-scoped triggering watches the paths declared on every Namespace above it.
/// A source report is about the Module only if it hashed all of them.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class InheritedTriggerPathTests(Fixture fixture) : IAsyncLifetime
{
    private NestedScenario _scenario = null!;
    private Module _module = null!;
    private string _sourceUrl = null!;

    public async Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        _sourceUrl = $"https://github.com/test/inherited-{Guid.NewGuid():N}";

        var parent = await _scenario.AddNamespace("00");
        var child = await _scenario.AddNamespace("00", parent.Id);

        await using (var db = fixture.CreateDbContext())
        {
            db.Add(new NamespaceAdditionalTriggerPath
            {
                Id = Guid.NewGuid(), OrganizationId = _scenario.OrganizationId, NamespaceId = parent.Id, Path = "shared/config"
            });
            await db.SaveChangesAsync();
        }

        _module = await _scenario.AddModule(child.Id, m =>
        {
            m.SourceUrl = _sourceUrl;
            m.SourceSubdirectory = "modules/app";
            m.TriggerOnSourceChanged = true;
            m.TriggerPathFilterEnabled = true;
        });
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Fact]
    public async Task A_report_that_left_out_a_path_declared_two_namespaces_up_is_not_about_the_module()
    {
        var published = await Consume([new PathHash { Path = "modules/app", TreeHash = "a1" }]);

        Assert.DoesNotContain(published, m => m.ModuleId == _module.Id);
    }

    [Fact]
    public async Task A_report_that_hashed_the_inherited_path_too_triggers_the_module_on_both()
    {
        List<PathHash> hashes =
        [
            new() { Path = "modules/app", TreeHash = "a1" },
            new() { Path = "shared/config", TreeHash = "b2" }
        ];

        var published = await Consume(hashes);

        var request = Assert.Single(published, m => m.ModuleId == _module.Id);
        Assert.Equal(
            TriggerPathClosure.Compose(["modules/app", "shared/config"], hashes.ToDictionary(h => h.Path, h => h.TreeHash)),
            request.DesiredClosureHash);
    }

    private async Task<List<GatekeepingJobRequested>> Consume(List<PathHash> pathHashes)
    {
        var published = new List<GatekeepingJobRequested>();

        var context = new Mock<ConsumeContext<SourceRefreshCompleted>>();
        context.SetupGet(c => c.Message).Returns(new SourceRefreshCompleted
        {
            OrganizationId = _scenario.OrganizationId,
            SourceUrl = _sourceUrl,
            SourceRevision = "main",
            DefinitiveRevision = "abc123",
            PathHashes = pathHashes
        });
        context
            .Setup(c => c.Publish(It.IsAny<GatekeepingJobRequested>(), It.IsAny<IPipe<PublishContext<GatekeepingJobRequested>>>(), It.IsAny<CancellationToken>()))
            .Callback<GatekeepingJobRequested, IPipe<PublishContext<GatekeepingJobRequested>>, CancellationToken>((message, _, _) => published.Add(message))
            .Returns(Task.CompletedTask);

        await using var db = fixture.CreateDbContext();
        await new SourceRefreshCompletedCompetingConsumer(db, NullLogger<SourceRefreshCompletedCompetingConsumer>.Instance)
            .Consume(context.Object);

        return published;
    }
}
