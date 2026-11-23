// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Moq;
using SnapCd.Contracts;
using SnapCd.Server.Core.Consumers.System.Competing;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// A change to a Namespace changes what every Module below it resolves, so the Modules it
/// triggers are all of those and not only the ones placed directly in it.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class NamespaceModifiedFanOutTests(Fixture fixture) : IAsyncLifetime
{
    private NestedScenario _scenario = null!;

    public Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Fact]
    public async Task A_modified_namespace_triggers_the_modules_two_namespaces_below_it()
    {
        var root = await _scenario.AddNamespace(
            "00", configure: n => n.TriggerBehaviourOnModified = NamespaceTriggerBehaviour.TriggerAllImmediately);
        var middle = await _scenario.AddNamespace("00", root.Id);
        var leaf = await _scenario.AddNamespace("00", middle.Id);

        var direct = await _scenario.AddModule(root.Id, m => m.TriggerOnDefinitionChanged = true);
        var nested = await _scenario.AddModule(leaf.Id, m => m.TriggerOnDefinitionChanged = true);

        var published = await Consume(root.Id);

        Assert.Contains(published, m => m.ModuleId == direct.Id);
        Assert.Contains(published, m => m.ModuleId == nested.Id);
    }

    [Fact]
    public async Task A_modified_namespace_does_not_trigger_a_module_outside_it()
    {
        var root = await _scenario.AddNamespace(
            "00", configure: n => n.TriggerBehaviourOnModified = NamespaceTriggerBehaviour.TriggerAllImmediately);
        var elsewhere = await _scenario.AddNamespace("00");
        var outside = await _scenario.AddModule(elsewhere.Id, m => m.TriggerOnDefinitionChanged = true);

        var published = await Consume(root.Id);

        Assert.DoesNotContain(published, m => m.ModuleId == outside.Id);
    }

    private async Task<List<GatekeepingJobRequested>> Consume(Guid namespaceId)
    {
        var published = new List<GatekeepingJobRequested>();

        var bus = new Mock<IBus>();
        bus.Setup(b => b.Publish(
                It.IsAny<GatekeepingJobRequested>(),
                It.IsAny<IPipe<PublishContext<GatekeepingJobRequested>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<GatekeepingJobRequested, IPipe<PublishContext<GatekeepingJobRequested>>, CancellationToken>(
                (message, _, _) => published.Add(message))
            .Returns(Task.CompletedTask);

        var context = new Mock<ConsumeContext<NamespaceModifiedEvent>>();
        context.Setup(c => c.Message).Returns(
            new NamespaceModifiedEvent { Id = namespaceId, OrganizationId = _scenario.OrganizationId });

        await using var db = fixture.CreateDbContext();
        await new NamespaceModifiedCompetingConsumer(db, bus.Object).Consume(context.Object);

        return published;
    }
}
