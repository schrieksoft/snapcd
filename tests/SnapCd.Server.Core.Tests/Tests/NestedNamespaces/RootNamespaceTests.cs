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
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// A Namespace with nothing above it is a root. Until the Stack table goes, a root carries a Stack
/// row of its own id, which the Namespace repository writes and nothing reads.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class RootNamespaceTests(Fixture fixture) : IAsyncLifetime
{
    private NestedScenario _scenario = null!;
    private NamespaceSecuredRepository _asOwner = null!;

    public Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        _asOwner = _scenario.NamespaceRepositoryFor(fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Fact]
    public async Task A_namespace_created_with_nothing_above_it_is_a_root_with_a_stack_row_of_its_own_id()
    {
        var root = await CreateRoot();

        Assert.Null(root.ParentNamespaceId);
        Assert.Equal(root.Id, root.StackId);
        Assert.Equal(root.Name, await StackName(root.Id));
    }

    [Fact]
    public async Task A_root_is_not_created_by_someone_with_no_role_on_the_organization()
    {
        var user = await _scenario.AddUser();
        var repository = _scenario.NamespaceRepositoryFor(user.Id);

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => repository.Create(New()));
    }

    [Fact]
    public async Task A_namespace_under_a_root_belongs_to_that_root()
    {
        var root = await CreateRoot();
        var child = await Create(New(parentId: root.Id));
        var grandchild = await Create(New(parentId: child.Id));

        Assert.Equal(root.Id, child.StackId);
        Assert.Equal(root.Id, grandchild.StackId);
    }

    [Fact]
    public async Task A_namespace_created_by_naming_only_the_stack_goes_under_the_root_of_that_id()
    {
        var root = await CreateRoot();
        var child = await Create(New(stackId: root.Id));

        Assert.Equal(root.Id, child.ParentNamespaceId);
        Assert.Equal([root.Id], await _scenario.AncestorsOf(child.Id));
    }

    [Fact]
    public async Task Renaming_a_root_renames_its_stack_row()
    {
        var root = await CreateRoot();
        var stored = await _scenario.Load(root.Id);
        stored.Name = $"renamed_{Guid.NewGuid():N}";

        await _asOwner.Update(stored);

        Assert.Equal(stored.Name, await StackName(root.Id));
    }

    [Fact]
    public async Task A_root_moved_under_another_loses_its_stack_row_and_takes_what_is_below_it_along()
    {
        var destination = await CreateRoot();
        var moving = await CreateRoot();
        var below = await Create(New(parentId: moving.Id));

        var stored = await _scenario.Load(moving.Id);
        stored.ParentNamespaceId = destination.Id;
        await _asOwner.Update(stored);

        Assert.Null(await StackName(moving.Id));
        Assert.Equal(destination.Id, (await _scenario.Load(moving.Id)).StackId);
        Assert.Equal(destination.Id, (await _scenario.Load(below.Id)).StackId);
    }

    [Fact]
    public async Task A_namespace_moved_to_the_top_becomes_a_root_with_a_stack_row()
    {
        var root = await CreateRoot();
        var moving = await Create(New(parentId: root.Id));
        var below = await Create(New(parentId: moving.Id));

        var stored = await _scenario.Load(moving.Id);
        stored.ParentNamespaceId = null;
        await _asOwner.Update(stored);

        Assert.Equal(moving.Name, await StackName(moving.Id));
        Assert.Equal(moving.Id, (await _scenario.Load(moving.Id)).StackId);
        Assert.Equal(moving.Id, (await _scenario.Load(below.Id)).StackId);
    }

    [Fact]
    public async Task Deleting_a_root_deletes_its_stack_row()
    {
        var root = await CreateRoot();

        await _asOwner.Delete(root.Id, _scenario.OrganizationId);

        Assert.Null(await StackName(root.Id));
    }

    private Task<Namespace> CreateRoot() => Create(New());

    private async Task<Namespace> Create(Namespace ns)
    {
        var created = await _asOwner.Create(ns);
        _scenario.Track(created);
        return await _scenario.Load(created.Id);
    }

    private Namespace New(Guid? parentId = null, Guid? stackId = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = $"nested_{Guid.NewGuid():N}",
        OrganizationId = _scenario.OrganizationId,
        ParentNamespaceId = parentId,
        StackId = stackId ?? Guid.Empty
    };

    private async Task<string?> StackName(Guid id)
    {
        await using var db = fixture.CreateDbContext();
        return await db.Stacks.Where(s => s.Id == id).Select(s => s.Name).FirstOrDefaultAsync();
    }
}
