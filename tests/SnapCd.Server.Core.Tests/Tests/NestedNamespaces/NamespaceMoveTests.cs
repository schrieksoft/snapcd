// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// Where a Namespace may be placed: under any Namespace that is not itself or inside it, as long
/// as nothing ends up deeper than the ancestry closure records.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class NamespaceMoveTests(Fixture fixture) : IAsyncLifetime
{
    private NestedScenario _scenario = null!;
    private Guid _owner;

    public Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        _owner = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Fact]
    public async Task A_namespace_moves_under_another_and_takes_what_is_below_it_along()
    {
        var from = await _scenario.AddNamespace("00");
        var moved = await _scenario.AddNamespace("00", from.Id);
        var below = await _scenario.AddNamespace("00", moved.Id);
        var to = await _scenario.AddNamespace("00");

        await Move(moved.Id, to.Id, _owner);

        Assert.Equal([to.Id], await _scenario.AncestorsOf(moved.Id));
        Assert.Equal([moved.Id, to.Id], await _scenario.AncestorsOf(below.Id));
    }

    [Fact]
    public async Task A_namespace_moves_out_to_become_a_root()
    {
        var parent = await _scenario.AddNamespace("00");
        var moved = await _scenario.AddNamespace("00", parent.Id);

        await Move(moved.Id, null, _owner);

        Assert.Empty(await _scenario.AncestorsOf(moved.Id));
    }

    [Fact]
    public async Task A_namespace_cannot_move_under_itself()
    {
        var ns = await _scenario.AddNamespace("00");

        await Assert.ThrowsAsync<InvalidNamespaceRefereceException>(() => Move(ns.Id, ns.Id, _owner));
    }

    [Fact]
    public async Task A_namespace_cannot_move_under_one_that_is_inside_it()
    {
        var top = await _scenario.AddNamespace("00");
        var middle = await _scenario.AddNamespace("00", top.Id);
        var bottom = await _scenario.AddNamespace("00", middle.Id);

        await Assert.ThrowsAsync<InvalidNamespaceRefereceException>(() => Move(top.Id, bottom.Id, _owner));
        Assert.Empty(await _scenario.AncestorsOf(top.Id));
    }

    [Fact]
    public async Task A_namespace_cannot_be_created_deeper_than_the_limit()
    {
        var deepest = await Chain(NamespaceRepository.MaxDepth + 1);

        var tooDeep = new Namespace
        {
            Id = Guid.NewGuid(),
            Name = $"nested_{Guid.NewGuid():N}",
            StackId = fixture.Stacks["00"].Id,
            OrganizationId = _scenario.OrganizationId,
            ParentNamespaceId = deepest
        };

        var refusal = await Record.ExceptionAsync(() => _scenario.NamespaceRepositoryFor(_owner).Create(tooDeep));
        if (refusal is null) _scenario.Track(tooDeep);

        Assert.IsType<InvalidNamespaceRefereceException>(refusal);
        Assert.Contains(NamespaceRepository.MaxDepth.ToString(), refusal.Message);
    }

    [Fact]
    public async Task A_namespace_cannot_move_to_where_what_is_below_it_would_pass_the_limit()
    {
        var deepest = await Chain(NamespaceRepository.MaxDepth);
        var moved = await _scenario.AddNamespace("00");
        await _scenario.AddNamespace("00", moved.Id);

        await Assert.ThrowsAsync<InvalidNamespaceRefereceException>(() => Move(moved.Id, deepest, _owner));
    }

    [Fact]
    public async Task Moving_needs_the_permission_to_nest_under_the_new_parent()
    {
        var moved = await _scenario.AddNamespace("00");
        var to = await _scenario.AddNamespace("00");
        var user = await _scenario.AddUser();
        await _scenario.Grant(user.Id, moved.Id, NamespaceRole.Owner);

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => Move(moved.Id, to.Id, user.Id));

        await _scenario.Grant(user.Id, to.Id, NamespaceRole.NestedNamespaceCreator);

        await Move(moved.Id, to.Id, user.Id);
        Assert.Equal([to.Id], await _scenario.AncestorsOf(moved.Id));
    }

    /// <summary>A line of Namespaces, each under the one before. Returns the last.</summary>
    private async Task<Guid> Chain(int length)
    {
        Guid? parent = null;

        for (var i = 0; i < length; i++) parent = (await _scenario.AddNamespace("00", parent)).Id;

        return parent!.Value;
    }

    private async Task Move(Guid namespaceId, Guid? newParentId, Guid userId)
    {
        var ns = await _scenario.Load(namespaceId);
        ns.ParentNamespaceId = newParentId;

        await _scenario.NamespaceRepositoryFor(userId).Update(ns);
    }
}
