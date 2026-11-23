// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.OutputSupplies;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>What is supplied to a Namespace, and whether a Module in a Namespace below it receives it.</summary>
[Collection("NewRoleBasedSharedFixture")]
public class SupplyInheritanceTests(Fixture fixture) : IAsyncLifetime
{
    private NestedScenario _scenario = null!;

    public Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Fact]
    public async Task A_runner_supplied_to_a_namespace_runs_a_module_placed_directly_in_it()
    {
        var parent = await _scenario.AddNamespace("00");
        var module = await _scenario.AddModule(parent.Id);
        var servicePrincipalId = await SupplyRunnerTo(parent.Id);

        Assert.True(await Runners().CanRunModule(servicePrincipalId, module.Id, _scenario.OrganizationId));
    }

    [Fact]
    public async Task A_runner_supplied_to_a_namespace_runs_a_module_in_a_namespace_below_it()
    {
        var parent = await _scenario.AddNamespace("00");
        var child = await _scenario.AddNamespace("00", parent.Id);
        var module = await _scenario.AddModule(child.Id);
        var servicePrincipalId = await SupplyRunnerTo(parent.Id);

        Assert.True(await Runners().CanRunModule(servicePrincipalId, module.Id, _scenario.OrganizationId));
    }

    [Fact]
    public async Task An_output_set_supplied_to_a_namespace_reaches_a_module_in_a_namespace_below_it()
    {
        var producerNamespace = await _scenario.AddNamespace("00");
        var producer = await _scenario.AddModule(
            producerNamespace.Id, m => m.OutputSetDownstreamRestriction = OutputSetDownstreamRestriction.All);
        var outputs = await _scenario.AddOutputs(producer.Id);

        var parent = await _scenario.AddNamespace("00");
        var child = await _scenario.AddNamespace("00", parent.Id);
        var consumer = await _scenario.AddModule(child.Id);

        await using (var db = fixture.CreateDbContext())
        {
            db.OutputSetNamespaceSupplies.Add(new OutputSetNamespaceSupply
            {
                Id = Guid.NewGuid(),
                OrganizationId = _scenario.OrganizationId,
                ModuleId = producer.Id,
                ConsumingNamespaceId = parent.Id
            });
            await db.SaveChangesAsync();
        }

        var readable = await _scenario.Checker().FilterReadable(outputs, consumer.Id, _scenario.OrganizationId);

        Assert.Equal(2, readable.Count);
    }

    private async Task<Guid> SupplyRunnerTo(Guid namespaceId)
    {
        var runner = await _scenario.AddRunner();
        Assert.False(runner.IsSuppliedToAllModules);

        await using var db = fixture.CreateDbContext();
        db.RunnerNamespaceSupplies.Add(new RunnerNamespaceSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = _scenario.OrganizationId,
            RunnerId = runner.Id,
            NamespaceId = namespaceId
        });
        await db.SaveChangesAsync();

        return runner.ServicePrincipalId;
    }

    private ServicePrincipalRepository Runners() => new(
        fixture.CreateDbContext(),
        _scenario.Owner(),
        fixture.CreateMockBus(),
        Options.Create(new ServicePrincipalRepositorySettings()));
}
