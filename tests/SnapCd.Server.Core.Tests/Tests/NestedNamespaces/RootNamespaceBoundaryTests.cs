// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// A root Namespace that withholds everything and exempts itself and its descendants is a closed
/// boundary: every Module under it reads every other, and nothing outside reads any of them.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class RootNamespaceBoundaryTests(Fixture fixture) : IAsyncLifetime
{
    private NestedScenario _scenario = null!;
    private Namespace _root = null!;
    private List<Output> _outputs = null!;

    public async Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);

        _root = await _scenario.AddNamespace("00", configure: n =>
        {
            n.DefaultOutputSetDownstreamRestriction = OutputSetDownstreamRestriction.All;
            n.DefaultOutputRestrictionExemption = OutputRestrictionExemption.OwnNamespaceAndDescendants;
        });

        var producerNamespace = await _scenario.AddNamespace("00", _root.Id);
        var producer = await _scenario.AddModule(producerNamespace.Id);
        _outputs = await _scenario.AddOutputs(producer.Id);
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Fact]
    public async Task A_module_reads_one_in_a_sibling_namespace_under_the_same_root()
    {
        var sibling = await _scenario.AddNamespace("00", _root.Id);
        var consumer = await _scenario.AddModule(sibling.Id);

        var readable = await _scenario.Checker().FilterReadable(_outputs, consumer.Id, _scenario.OrganizationId);

        Assert.Equal(2, readable.Count);
    }

    [Fact]
    public async Task A_module_directly_in_the_root_reads_one_in_a_namespace_below_it()
    {
        var consumer = await _scenario.AddModule(_root.Id);

        var readable = await _scenario.Checker().FilterReadable(_outputs, consumer.Id, _scenario.OrganizationId);

        Assert.Equal(2, readable.Count);
    }

    [Fact]
    public async Task A_module_outside_the_root_reads_nothing()
    {
        var outside = await _scenario.AddNamespace("00");
        var consumer = await _scenario.AddModule(outside.Id);

        var readable = await _scenario.Checker().FilterReadable(_outputs, consumer.Id, _scenario.OrganizationId);

        Assert.Empty(readable);
    }
}
