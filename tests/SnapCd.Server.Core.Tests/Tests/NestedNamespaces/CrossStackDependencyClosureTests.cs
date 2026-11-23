// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// The gate orders jobs from the dependency closure, so a reference that resolves has to be in it.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class CrossStackDependencyClosureTests(Fixture fixture) : IAsyncLifetime
{
    private const byte ApplyOrder = 1;
    private const byte DestroyOrder = 2;

    private NestedScenario _scenario = null!;

    public Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Fact]
    public async Task A_reference_within_one_stack_is_in_the_closure_in_both_orders()
    {
        var (consumer, producer) = await Reference(consumerStack: "00", producerStack: "00");

        Assert.True(await InClosure(ApplyOrder, root: consumer, consumer, producer));
        Assert.True(await InClosure(DestroyOrder, root: producer, consumer, producer));
    }

    [Fact]
    public async Task A_reference_across_two_stacks_is_a_direct_edge()
    {
        var (consumer, producer) = await Reference(consumerStack: "01", producerStack: "00");

        await using var db = fixture.CreateDbContext();
        Assert.True(await db.DependencyEdges.AnyAsync(
            e => e.DefinedModuleId == consumer && e.ReferencedModuleId == producer));
    }

    [Fact]
    public async Task A_reference_across_two_stacks_is_in_the_closure_in_apply_order()
    {
        var (consumer, producer) = await Reference(consumerStack: "01", producerStack: "00");

        Assert.True(await InClosure(ApplyOrder, root: consumer, consumer, producer));
    }

    [Fact]
    public async Task A_reference_across_two_stacks_is_in_the_closure_in_destroy_order()
    {
        var (consumer, producer) = await Reference(consumerStack: "01", producerStack: "00");

        Assert.True(await InClosure(DestroyOrder, root: producer, consumer, producer));
    }

    private async Task<(Guid Consumer, Guid Producer)> Reference(string consumerStack, string producerStack)
    {
        var producerNamespace = await _scenario.AddNamespace(producerStack);
        var consumerNamespace = await _scenario.AddNamespace(consumerStack);
        var producer = await _scenario.AddModule(producerNamespace.Id);
        var consumer = await _scenario.AddModule(consumerNamespace.Id);

        await using var db = fixture.CreateDbContext();
        db.ModuleInputs.Add(new ModuleParamFromOutput
        {
            Id = Guid.NewGuid(),
            Name = "vpc_id",
            ModuleId = consumer.Id,
            OutputModuleId = producer.Id,
            OutputName = "vpc_id",
            OrganizationId = _scenario.OrganizationId
        });
        await db.SaveChangesAsync();

        return (consumer.Id, producer.Id);
    }

    private async Task<bool> InClosure(byte direction, Guid root, Guid consumer, Guid producer)
    {
        await using var db = fixture.CreateDbContext();

        return await db.RecursiveDependencyEdges.AnyAsync(e =>
            e.Direction == direction
            && e.RootModuleId == root
            && e.DefinedModuleId == consumer
            && e.ReferencedModuleId == producer);
    }
}
