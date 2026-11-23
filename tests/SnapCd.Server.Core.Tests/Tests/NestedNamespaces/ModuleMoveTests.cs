// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// What stops a Module, or a Namespace, from being moved: only a target that does not exist. A
/// reference to another Module's Outputs does not, wherever the two end up, and neither does a
/// selection of a Namespace input, which no longer has any effect.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ModuleMoveTests(Fixture fixture) : IAsyncLifetime
{
    private NestedScenario _scenario = null!;

    public Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Fact]
    public async Task A_module_holding_a_selection_of_a_namespace_input_can_move_away_from_that_namespace()
    {
        var from = await _scenario.AddNamespace("00");
        var to = await _scenario.AddNamespace("00");
        var module = await _scenario.AddModule(from.Id);
        await Select(module, await AddInput(from.Id, "region"));

        await Move(module.Id, to.Id);

        Assert.Equal(to.Id, (await Stored(module.Id)).NamespaceId);
    }

    [Fact]
    public async Task A_module_cannot_move_to_a_namespace_that_does_not_exist()
    {
        var from = await _scenario.AddNamespace("00");
        var module = await _scenario.AddModule(from.Id);

        await Assert.ThrowsAsync<InvalidNamespaceRefereceException>(() => Move(module.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task A_module_that_reads_another_module_s_output_can_move_to_another_stack()
    {
        var producerNamespace = await _scenario.AddNamespace("00");
        var consumerNamespace = await _scenario.AddNamespace("00");
        var elsewhere = await _scenario.AddNamespace("01");
        var producer = await _scenario.AddModule(producerNamespace.Id);
        var consumer = await _scenario.AddModule(consumerNamespace.Id);
        await Reference(consumer, producer);

        await Move(consumer.Id, elsewhere.Id);

        Assert.Equal(elsewhere.Id, (await Stored(consumer.Id)).NamespaceId);
    }

    [Fact]
    public async Task A_namespace_whose_modules_are_read_from_its_stack_can_move_to_another_stack()
    {
        var moved = await _scenario.AddNamespace("00");
        var stays = await _scenario.AddNamespace("00");
        var producer = await _scenario.AddModule(moved.Id);
        var consumer = await _scenario.AddModule(stays.Id);
        await Reference(consumer, producer);

        var stored = await _scenario.Load(moved.Id);
        stored.StackId = fixture.Stacks["01"].Id;
        var owner = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id;
        await _scenario.NamespaceRepositoryFor(owner).Update(stored);

        Assert.Equal(fixture.Stacks["01"].Id, (await _scenario.Load(moved.Id)).StackId);
    }

    private async Task Move(Guid moduleId, Guid namespaceId)
    {
        await using var db = fixture.CreateDbContext();
        var repository = new ModuleRepository(db, _scenario.Owner(), fixture.CreateMockBus(), fixture.CreateModuleSettings());

        var module = await repository.Get(moduleId, _scenario.OrganizationId);
        module.NamespaceId = namespaceId;
        await repository.ExecuteUpdate(module);
        await db.SaveChangesAsync();
    }

    private async Task<Module> Stored(Guid moduleId)
    {
        await using var db = fixture.CreateDbContext();
        return await db.Modules.AsNoTracking().SingleAsync(m => m.Id == moduleId);
    }

    private async Task<NamespaceInput> AddInput(Guid namespaceId, string name)
    {
        var input = new NamespaceParamFromLiteral
        {
            Id = Guid.NewGuid(), Name = name, NamespaceId = namespaceId, OrganizationId = _scenario.OrganizationId,
            LiteralValue = "eu-west-1", UsageMode = NamespaceInputUsageMode.UseIfSelected
        };

        await using var db = fixture.CreateDbContext();
        db.NamespaceInputs.Add(input);
        await db.SaveChangesAsync();
        return input;
    }

    private async Task Select(Module module, NamespaceInput input)
    {
        await using var db = fixture.CreateDbContext();
        db.ModuleInputs.Add(new ModuleParamFromNamespace
        {
            Id = Guid.NewGuid(), Name = input.Name, ModuleId = module.Id,
            NamespaceInputId = input.Id, OrganizationId = _scenario.OrganizationId
        });
        await db.SaveChangesAsync();
    }

    private async Task Reference(Module consumer, Module producer)
    {
        await using var db = fixture.CreateDbContext();
        db.ModuleInputs.Add(new ModuleParamFromOutput
        {
            Id = Guid.NewGuid(), Name = "vpc_id", ModuleId = consumer.Id,
            OutputModuleId = producer.Id, OutputName = "vpc_id", OrganizationId = _scenario.OrganizationId
        });
        await db.SaveChangesAsync();
    }
}
