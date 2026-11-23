// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Secrets.Scoped;
using SnapCd.Server.Core.Settings.Repositories;
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

    [Fact]
    public async Task The_nested_reader_reads_the_rows_of_a_namespace_below_and_changes_none()
    {
        await _scenario.Grant(_user.Id, _top.Id, NamespaceRole.NestedNamespaceReader);
        var below = await AddInput(_middle.Id);
        var own = await AddInput(_top.Id);
        var inputs = Inputs();

        Assert.True(inputs.CanRead(below.Id, _scenario.OrganizationId));

        Assert.False(inputs.CanRead(own.Id, _scenario.OrganizationId));
        Assert.False(inputs.CanUpdate(below.Id, _scenario.OrganizationId));
        Assert.False(inputs.CanDelete(below.Id, _scenario.OrganizationId));
        Assert.False(inputs.CanCreate(_middle.Id, _scenario.OrganizationId));
    }

    [Fact]
    public async Task The_nested_contributor_changes_the_rows_of_a_namespace_below_and_not_of_the_one_it_is_on()
    {
        await _scenario.Grant(_user.Id, _top.Id, NamespaceRole.NestedNamespaceContributor);
        var below = await AddInput(_bottom.Id);
        var own = await AddInput(_top.Id);
        var inputs = Inputs();

        Assert.True(inputs.CanRead(below.Id, _scenario.OrganizationId));
        Assert.True(inputs.CanUpdate(below.Id, _scenario.OrganizationId));
        Assert.True(inputs.CanDelete(below.Id, _scenario.OrganizationId));
        Assert.True(inputs.CanCreate(_bottom.Id, _scenario.OrganizationId));

        Assert.False(inputs.CanRead(own.Id, _scenario.OrganizationId));
        Assert.False(inputs.CanUpdate(own.Id, _scenario.OrganizationId));
        Assert.False(inputs.CanCreate(_top.Id, _scenario.OrganizationId));
    }

    [Theory]
    [InlineData(NamespaceRole.NestedNamespaceCreator)]
    [InlineData(NamespaceRole.NestedNamespaceContributor)]
    [InlineData(NamespaceRole.NestedNamespaceReader)]
    [InlineData(NamespaceRole.NestedNamespaceMetadataReader)]
    public async Task No_nested_role_reaches_a_module_in_a_namespace_below(NamespaceRole role)
    {
        await _scenario.Grant(_user.Id, _top.Id, role);
        var module = await _scenario.AddModule(_middle.Id);
        var principal = fixture.CreatePrincipalProvider(_user.Id, PrincipalDiscriminator.User, _scenario.OrganizationId);

        var modules = new ModuleSecuredRepository(
            new ModuleRepository(fixture.CreateDbContext(), principal, fixture.CreateMockBus(), fixture.CreateModuleSettings()),
            principal);

        Assert.False(modules.CanRead(module.Id, _scenario.OrganizationId));
        Assert.False(modules.CanReadMetadata(module.Id, _scenario.OrganizationId));
        Assert.False(modules.CanUpdate(module.Id, _scenario.OrganizationId));
        Assert.False(modules.CanCreate(_middle.Id, _scenario.OrganizationId));
    }

    [Theory]
    [InlineData(NamespaceRole.NestedNamespaceContributor)]
    [InlineData(NamespaceRole.NestedNamespaceReader)]
    public async Task No_nested_role_reads_a_secret_of_a_namespace_below(NamespaceRole role)
    {
        await _scenario.Grant(_user.Id, _top.Id, role);

        Guid secretId;
        await using (var db = fixture.CreateDbContext())
        {
            var secret = fixture.CreateTestNamespaceSecret($"nested_{Guid.NewGuid():N}", _middle.Id, _scenario.OrganizationId, db);
            await db.SaveChangesAsync();
            secretId = secret.Id;
        }

        var principal = fixture.CreatePrincipalProvider(_user.Id, PrincipalDiscriminator.User, _scenario.OrganizationId);
        var secrets = new NamespaceSecretSecuredRepository(
            new NamespaceSecretRepository(
                fixture.CreateDbContext(), principal, fixture.CreateMockBus(),
                Options.Create(new NamespaceSecretRepositorySettings())),
            principal);

        Assert.False(secrets.CanRead(secretId, _scenario.OrganizationId));
        Assert.False(secrets.CanCreate(_middle.Id, _scenario.OrganizationId));
    }

    private NamespaceInputFromLiteralSecuredRepository<NamespaceParamFromLiteral> Inputs()
    {
        var principal = fixture.CreatePrincipalProvider(_user.Id, PrincipalDiscriminator.User, _scenario.OrganizationId);

        return new NamespaceInputFromLiteralSecuredRepository<NamespaceParamFromLiteral>(
            new NamespaceInputFromLiteralRepository<NamespaceParamFromLiteral>(
                fixture.CreateDbContext(), principal, fixture.CreateMockBus(),
                Options.Create(new NamespaceInputFromLiteralRepositorySettings())),
            principal);
    }

    private async Task<NamespaceParamFromLiteral> AddInput(Guid namespaceId)
    {
        var input = new NamespaceParamFromLiteral
        {
            Id = Guid.NewGuid(),
            Name = $"input_{Guid.NewGuid():N}",
            NamespaceId = namespaceId,
            OrganizationId = _scenario.OrganizationId,
            LiteralValue = "value",
            UsageMode = NamespaceInputUsageMode.UseIfSelected
        };

        await using var db = fixture.CreateDbContext();
        db.NamespaceInputs.Add(input);
        await db.SaveChangesAsync();

        return input;
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
