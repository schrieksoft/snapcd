// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Services.Nonsecured.ParamResolver.Helpers;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.AgentSupplies;
using SnapCd.Server.Core.Entities.Definition.IntegrationSupplies;
using SnapCd.Server.Core.Entities.Definition.OutputSupplies;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.Nonsecured.Ai.Missions;
using SnapCd.Server.Core.Services.Nonsecured.Integrations;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// A supply to a Namespace reaches the Modules placed directly in it. Whether it also reaches the
/// Modules in the Namespaces below is the supply's own choice, and off unless it says so.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class SupplyInheritanceTests(Fixture fixture) : IAsyncLifetime
{
    private NestedScenario _scenario = null!;
    private Namespace _parent = null!;
    private Namespace _grandchild = null!;
    private Module _direct = null!;
    private Module _nested = null!;

    public async Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        _parent = await _scenario.AddNamespace("00");
        var child = await _scenario.AddNamespace("00", _parent.Id);
        _grandchild = await _scenario.AddNamespace("00", child.Id);
        _direct = await _scenario.AddModule(_parent.Id);
        _nested = await _scenario.AddModule(_grandchild.Id);
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task A_runner_supplied_to_a_namespace(bool inherited, bool reachesNested)
    {
        var runner = await _scenario.AddRunner();
        Assert.False(runner.IsSuppliedToAllModules);

        await Add(new RunnerNamespaceSupply
        {
            Id = Guid.NewGuid(), OrganizationId = _scenario.OrganizationId,
            RunnerId = runner.Id, NamespaceId = _parent.Id, InheritedByDescendants = inherited
        });

        var principals = new ServicePrincipalRepository(
            fixture.CreateDbContext(), _scenario.Owner(), fixture.CreateMockBus(),
            Options.Create(new ServicePrincipalRepositorySettings()));
        var runners = new RunnerRepository(
            fixture.CreateDbContext(), _scenario.Owner(), fixture.CreateMockBus(),
            Options.Create(new RunnerRepositorySettings()));

        Assert.True(await principals.CanRunModule(runner.ServicePrincipalId, _direct.Id, _scenario.OrganizationId));
        Assert.Equal(reachesNested, await principals.CanRunModule(runner.ServicePrincipalId, _nested.Id, _scenario.OrganizationId));

        Assert.Contains(await runners.ListAssignedToModule(_direct.Id, _scenario.OrganizationId), r => r.Id == runner.Id);
        Assert.Equal(reachesNested,
            (await runners.ListAssignedToModule(_nested.Id, _scenario.OrganizationId)).Any(r => r.Id == runner.Id));
        Assert.Equal(reachesNested,
            (await runners.ListAssignedToNamespace(_grandchild.Id, _scenario.OrganizationId)).Any(r => r.Id == runner.Id));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task An_agent_supplied_to_a_namespace(bool inherited, bool reachesNested)
    {
        var agentId = fixture.Agents["0"].Id;

        await Add(new AgentNamespaceSupply
        {
            Id = Guid.NewGuid(), OrganizationId = _scenario.OrganizationId,
            AgentId = agentId, NamespaceId = _parent.Id, InheritedByDescendants = inherited
        });

        using var resolver = new AgentSupplyResolver(fixture.CreateDbContext());

        Assert.True(await resolver.IsAgentSuppliedToModule(agentId, _direct.Id, _scenario.OrganizationId));
        Assert.Equal(reachesNested, await resolver.IsAgentSuppliedToModule(agentId, _nested.Id, _scenario.OrganizationId));
        Assert.Equal(reachesNested, await resolver.IsAgentSuppliedToNamespace(agentId, _grandchild.Id, _scenario.OrganizationId));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task An_integration_supplied_to_a_namespace(bool inherited, bool reachesNested)
    {
        var integrationId = fixture.Integrations["Unsupplied"].Id;

        await Add(new IntegrationNamespaceSupply
        {
            Id = Guid.NewGuid(), OrganizationId = _scenario.OrganizationId,
            IntegrationId = integrationId, NamespaceId = _parent.Id, InheritedByDescendants = inherited
        });

        var resolver = new IntegrationSupplyResolver(fixture.CreateDbContext());

        Assert.True(await resolver.IsIntegrationSuppliedToModule(integrationId, _direct.Id, _scenario.OrganizationId));
        Assert.Equal(reachesNested, await resolver.IsIntegrationSuppliedToModule(integrationId, _nested.Id, _scenario.OrganizationId));
        Assert.Equal(reachesNested, await resolver.IsIntegrationSuppliedToNamespace(integrationId, _grandchild.Id, _scenario.OrganizationId));
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 2)]
    public async Task An_output_set_supplied_to_a_namespace(bool inherited, int readableByNested)
    {
        var (producer, outputs) = await RestrictedProducer();

        await Add(new OutputSetNamespaceSupply
        {
            Id = Guid.NewGuid(), OrganizationId = _scenario.OrganizationId,
            ModuleId = producer.Id, ConsumingNamespaceId = _parent.Id, InheritedByDescendants = inherited
        });

        Assert.Equal(2, (await _scenario.Checker().FilterReadable(outputs, _direct.Id, _scenario.OrganizationId)).Count);
        Assert.Equal(readableByNested, (await _scenario.Checker().FilterReadable(outputs, _nested.Id, _scenario.OrganizationId)).Count);

        await using var db = fixture.CreateDbContext();
        var reached = OutputReadabilityFor(db, producer.Id);
        Assert.Contains(_direct.Id, reached);
        Assert.Equal(inherited, reached.Contains(_nested.Id));
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 1)]
    public async Task A_single_output_supplied_to_a_namespace(bool inherited, int readableByNested)
    {
        var (producer, outputs) = await RestrictedProducer();

        await Add(new OutputNamespaceSupply
        {
            Id = Guid.NewGuid(), OrganizationId = _scenario.OrganizationId,
            ModuleId = producer.Id, OutputName = "vpc_id", ConsumingNamespaceId = _parent.Id, InheritedByDescendants = inherited
        });

        Assert.Single(await _scenario.Checker().FilterReadable(outputs, _direct.Id, _scenario.OrganizationId));
        Assert.Equal(readableByNested, (await _scenario.Checker().FilterReadable(outputs, _nested.Id, _scenario.OrganizationId)).Count);
    }

    /// <summary>The consumers a changed Output Set of this producer reaches, among the two Modules here.</summary>
    private List<Guid> OutputReadabilityFor(SnapCdDbContext db, Guid producerId) =>
        OutputReadability
            .ReadableConsumerIds(db, producerId, _scenario.OrganizationId, [_direct.Id, _nested.Id], null)
            .ToList();

    private async Task<(Module Producer, List<Output> Outputs)> RestrictedProducer()
    {
        var elsewhere = await _scenario.AddNamespace("00");
        var producer = await _scenario.AddModule(
            elsewhere.Id, m => m.OutputSetDownstreamRestriction = OutputSetDownstreamRestriction.All);

        return (producer, await _scenario.AddOutputs(producer.Id));
    }

    private async Task Add<T>(T supply) where T : class
    {
        await using var db = fixture.CreateDbContext();
        db.Add(supply);
        await db.SaveChangesAsync();
    }
}
