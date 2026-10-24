// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using SnapCd.Contracts;
using SnapCd.Server.Core.Consumers.System.Competing;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Consumers;

[Collection("NewRoleBasedSharedFixture")]
public class StackModifiedCompetingConsumerTests
{
    private readonly Fixture _fixture;

    public StackModifiedCompetingConsumerTests(Fixture fixture) => _fixture = fixture;

    private async Task<List<GatekeepingJobRequested>> Consume(StackTriggerBehaviour behaviour)
    {
        var stack = _fixture.Stacks["00"];
        await using var db = _fixture.CreateDbContext();
        var entity = await db.Stacks.SingleAsync(s => s.Id == stack.Id);
        var original = entity.TriggerBehaviourOnModified;
        entity.TriggerBehaviourOnModified = behaviour;
        var flagged = new Dictionary<string, bool> { ["0000"] = true, ["0001"] = false, ["0100"] = true };
        var flaggedIds = flagged.Keys.Select(k => _fixture.Modules[k].Id).ToList();
        var modules = await db.Modules.Where(m => flaggedIds.Contains(m.Id)).ToListAsync();
        var originalFlags = modules.ToDictionary(m => m.Id, m => m.TriggerOnDefinitionChanged);
        foreach (var (key, value) in flagged)
            modules.Single(m => m.Id == _fixture.Modules[key].Id).TriggerOnDefinitionChanged = value;
        await db.SaveChangesAsync();

        try
        {
            var published = new List<GatekeepingJobRequested>();
            var bus = new Mock<IBus>();
            bus.Setup(b => b.Publish(It.IsAny<GatekeepingJobRequested>(), It.IsAny<IPipe<PublishContext<GatekeepingJobRequested>>>(), It.IsAny<CancellationToken>()))
                .Callback<GatekeepingJobRequested, IPipe<PublishContext<GatekeepingJobRequested>>, CancellationToken>((m, _, _) => published.Add(m))
                .Returns(Task.CompletedTask);
            var context = new Mock<ConsumeContext<StackModifiedEvent>>();
            context.Setup(c => c.Message).Returns(new StackModifiedEvent { Id = stack.Id, OrganizationId = stack.OrganizationId });

            await using var consumerDb = _fixture.CreateDbContext();
            await new StackModifiedCompetingConsumer(consumerDb, bus.Object).Consume(context.Object);
            return published;
        }
        finally
        {
            entity.TriggerBehaviourOnModified = original;
            foreach (var module in modules)
                module.TriggerOnDefinitionChanged = originalFlags[module.Id];
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task A_stack_set_to_trigger_all_triggers_every_module_in_its_namespaces_that_triggers_on_definition_changes()
    {
        var stack = _fixture.Stacks["00"];

        var published = await Consume(StackTriggerBehaviour.TriggerAllImmediately);

        Assert.Contains(published, m => m.ModuleId == _fixture.Modules["0000"].Id);
        Assert.DoesNotContain(published, m => m.ModuleId == _fixture.Modules["0001"].Id);
        Assert.DoesNotContain(published, m => m.ModuleId == _fixture.Modules["0100"].Id);
        Assert.All(published, m => Assert.Equal(stack.OrganizationId, m.OrganizationId));
    }

    [Fact]
    public async Task A_stack_set_not_to_trigger_triggers_nothing()
    {
        var published = await Consume(StackTriggerBehaviour.DoNotTrigger);

        Assert.Empty(published);
    }
}
