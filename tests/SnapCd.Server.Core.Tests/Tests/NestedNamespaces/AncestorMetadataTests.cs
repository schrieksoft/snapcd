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
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// A role on something contained lets its holder see the name of what contains it, so that they
/// can navigate to what they do have access to. Under nesting the path runs through every ancestor.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class AncestorMetadataTests(Fixture fixture) : IAsyncLifetime
{
    private NestedScenario _scenario = null!;
    private Namespace _root = null!;
    private Namespace _leaf = null!;
    private Module _module = null!;
    private User _user = null!;

    public async Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        _root = await _scenario.AddNamespace("00");
        var middle = await _scenario.AddNamespace("00", _root.Id);
        _leaf = await _scenario.AddNamespace("00", middle.Id);
        _module = await _scenario.AddModule(_leaf.Id);
        _user = await _scenario.AddUser();
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Fact]
    public async Task A_role_on_a_module_grants_metadata_read_on_the_namespace_it_is_in()
    {
        await GrantOnModule();

        Assert.True(_scenario.NamespaceRepositoryFor(_user.Id).CanReadMetadata(_leaf.Id, _scenario.OrganizationId));
    }

    [Fact]
    public async Task A_role_on_a_module_grants_metadata_read_on_a_namespace_two_above_it()
    {
        await GrantOnModule();

        Assert.True(_scenario.NamespaceRepositoryFor(_user.Id).CanReadMetadata(_root.Id, _scenario.OrganizationId));
    }

    [Fact]
    public async Task A_role_on_a_namespace_grants_metadata_read_on_a_namespace_two_above_it()
    {
        await using (var db = fixture.CreateDbContext())
        {
            db.UserNamespaceRoleAssignments.Add(new UserNamespaceRoleAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = _scenario.OrganizationId,
                NamespaceId = _leaf.Id,
                UserId = _user.Id,
                RoleName = NamespaceRole.Reader
            });
            await db.SaveChangesAsync();
        }

        Assert.True(_scenario.NamespaceRepositoryFor(_user.Id).CanReadMetadata(_root.Id, _scenario.OrganizationId));
    }

    [Fact]
    public async Task A_role_on_a_namespace_does_not_grant_read_on_a_namespace_above_it()
    {
        await using (var db = fixture.CreateDbContext())
        {
            db.UserNamespaceRoleAssignments.Add(new UserNamespaceRoleAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = _scenario.OrganizationId,
                NamespaceId = _leaf.Id,
                UserId = _user.Id,
                RoleName = NamespaceRole.Reader
            });
            await db.SaveChangesAsync();
        }

        Assert.False(_scenario.NamespaceRepositoryFor(_user.Id).CanRead(_root.Id, _scenario.OrganizationId));
    }

    private async Task GrantOnModule()
    {
        await using var db = fixture.CreateDbContext();
        db.UserModuleRoleAssignments.Add(new UserModuleRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = _scenario.OrganizationId,
            ModuleId = _module.Id,
            UserId = _user.Id,
            RoleName = ModuleRole.Reader
        });
        await db.SaveChangesAsync();
    }
}
