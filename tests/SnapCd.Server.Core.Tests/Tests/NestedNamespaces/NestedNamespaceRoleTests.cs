// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// The NestedNamespace roles act on the Namespaces below the one they are granted on, and on
/// nothing in them: not the Modules, and not the Namespace the role sits on.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class NestedNamespaceRoleTests(Fixture fixture) : IAsyncLifetime
{
    private NestedScenario _scenario = null!;
    private Namespace _top = null!;
    private Namespace _middle = null!;
    private Namespace _bottom = null!;
    private User _user = null!;

    public async Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        _top = await _scenario.AddNamespace("00");
        _middle = await _scenario.AddNamespace("00", _top.Id);
        _bottom = await _scenario.AddNamespace("00", _middle.Id);
        _user = await _scenario.AddUser();
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Theory]
    [InlineData(NamespaceRole.Owner)]
    [InlineData(NamespaceRole.Contributor)]
    [InlineData(NamespaceRole.NestedNamespaceCreator)]
    [InlineData(NamespaceRole.NestedNamespaceContributor)]
    public async Task These_roles_on_a_namespace_allow_nesting_under_it_and_under_what_is_below_it(NamespaceRole role)
    {
        await _scenario.Grant(_user.Id, _top.Id, role);

        Assert.Null(await Record.ExceptionAsync(() => CreateUnder(_top.Id)));
        Assert.Null(await Record.ExceptionAsync(() => CreateUnder(_bottom.Id)));
    }

    [Theory]
    [InlineData(NamespaceRole.Reader)]
    [InlineData(NamespaceRole.MetadataReader)]
    [InlineData(NamespaceRole.ModuleCreator)]
    [InlineData(NamespaceRole.IdentityAccessManager)]
    [InlineData(NamespaceRole.JobManager)]
    [InlineData(NamespaceRole.NestedNamespaceReader)]
    [InlineData(NamespaceRole.NestedNamespaceMetadataReader)]
    public async Task These_roles_on_a_namespace_do_not_allow_nesting_under_it(NamespaceRole role)
    {
        await _scenario.Grant(_user.Id, _top.Id, role);

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => CreateUnder(_top.Id));
    }

    [Fact]
    public async Task A_role_on_a_namespace_does_not_allow_nesting_under_the_one_above_it()
    {
        await _scenario.Grant(_user.Id, _middle.Id, NamespaceRole.Owner);

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => CreateUnder(_top.Id));
    }

    [Fact]
    public async Task The_stack_role_that_creates_a_root_namespace_does_not_allow_nesting_in_that_stack()
    {
        await using (var db = fixture.CreateDbContext())
        {
            db.UserStackRoleAssignments.Add(new UserStackRoleAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = _scenario.OrganizationId,
                StackId = fixture.Stacks["00"].Id,
                UserId = _user.Id,
                RoleName = StackRole.NamespaceCreator
            });
            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => CreateUnder(_top.Id));
        Assert.Null(await Record.ExceptionAsync(() => CreateUnder(null)));
    }

    [Fact]
    public async Task The_nested_contributor_updates_and_deletes_what_is_below_and_not_the_namespace_it_is_on()
    {
        await _scenario.Grant(_user.Id, _top.Id, NamespaceRole.NestedNamespaceContributor);
        var repository = _scenario.NamespaceRepositoryFor(_user.Id);

        Assert.True(repository.CanUpdate(_middle.Id, _scenario.OrganizationId));
        Assert.True(repository.CanDelete(_bottom.Id, _scenario.OrganizationId));
        Assert.True(repository.CanRead(_bottom.Id, _scenario.OrganizationId));

        Assert.False(repository.CanUpdate(_top.Id, _scenario.OrganizationId));
        Assert.False(repository.CanDelete(_top.Id, _scenario.OrganizationId));
        Assert.False(repository.CanRead(_top.Id, _scenario.OrganizationId));
    }

    [Fact]
    public async Task The_nested_reader_reads_what_is_below_and_changes_nothing()
    {
        await _scenario.Grant(_user.Id, _top.Id, NamespaceRole.NestedNamespaceReader);
        var repository = _scenario.NamespaceRepositoryFor(_user.Id);

        Assert.True(repository.CanRead(_middle.Id, _scenario.OrganizationId));
        Assert.True(repository.CanRead(_bottom.Id, _scenario.OrganizationId));

        Assert.False(repository.CanRead(_top.Id, _scenario.OrganizationId));
        Assert.False(repository.CanUpdate(_middle.Id, _scenario.OrganizationId));
    }

    [Fact]
    public async Task The_nested_metadata_reader_sees_names_below_and_reads_nothing()
    {
        await _scenario.Grant(_user.Id, _top.Id, NamespaceRole.NestedNamespaceMetadataReader);
        var repository = _scenario.NamespaceRepositoryFor(_user.Id);

        Assert.True(repository.CanReadMetadata(_bottom.Id, _scenario.OrganizationId));
        Assert.False(repository.CanRead(_bottom.Id, _scenario.OrganizationId));
    }

    private async Task CreateUnder(Guid? parentId)
    {
        var child = new Namespace
        {
            Id = Guid.NewGuid(),
            Name = $"nested_{Guid.NewGuid():N}",
            StackId = fixture.Stacks["00"].Id,
            OrganizationId = _scenario.OrganizationId,
            ParentNamespaceId = parentId
        };

        await _scenario.NamespaceRepositoryFor(_user.Id).Create(child);
        _scenario.Track(child);
    }
}
