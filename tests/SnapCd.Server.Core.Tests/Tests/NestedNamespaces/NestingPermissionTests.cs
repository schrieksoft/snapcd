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
/// A Module inherits every ancestor's inputs, secret-backed ones included, so who may place a
/// Namespace under another decides who may read what that Namespace holds.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class NestingPermissionTests(Fixture fixture) : IAsyncLifetime
{
    private NestedScenario _scenario = null!;

    public Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Fact]
    public async Task A_namespace_cannot_be_nested_under_a_parent_the_principal_has_no_role_on()
    {
        var parent = await _scenario.AddNamespace("00");
        var user = await _scenario.AddUser();

        await using (var db = fixture.CreateDbContext())
        {
            db.UserStackRoleAssignments.Add(new UserStackRoleAssignment
            {
                Id = Guid.NewGuid(),
                OrganizationId = _scenario.OrganizationId,
                StackId = fixture.Stacks["01"].Id,
                UserId = user.Id,
                RoleName = StackRole.NamespaceCreator
            });
            await db.SaveChangesAsync();
        }

        var child = new Namespace
        {
            Id = Guid.NewGuid(),
            Name = $"nested_{Guid.NewGuid():N}",
            StackId = fixture.Stacks["01"].Id,
            OrganizationId = _scenario.OrganizationId,
            ParentNamespaceId = parent.Id
        };

        var refusal = await Record.ExceptionAsync(() => _scenario.NamespaceRepositoryFor(user.Id).Create(child));

        if (refusal is null) _scenario.Track(child);

        Assert.IsType<PrincipalNotAuthorizedException>(refusal);
    }

    [Fact]
    public async Task A_module_resolves_the_inputs_declared_two_namespaces_above_it()
    {
        var root = await _scenario.AddNamespace("00");
        var middle = await _scenario.AddNamespace("00", root.Id);
        var leaf = await _scenario.AddNamespace("00", middle.Id);
        var module = await _scenario.AddModule(leaf.Id);

        await using (var db = fixture.CreateDbContext())
        {
            var secret = fixture.CreateTestNamespaceSecret(
                $"nested_{Guid.NewGuid():N}", root.Id, _scenario.OrganizationId, db);

            db.NamespaceInputs.Add(new NamespaceParamFromSecret
            {
                Id = Guid.NewGuid(),
                Name = "db_password",
                NamespaceId = root.Id,
                SecretId = secret.Id,
                OrganizationId = _scenario.OrganizationId,
                UsageMode = NamespaceInputUsageMode.UseIfSelected
            });
            db.NamespaceInputs.Add(new NamespaceParamFromLiteral
            {
                Id = Guid.NewGuid(),
                Name = "region",
                NamespaceId = root.Id,
                OrganizationId = _scenario.OrganizationId,
                LiteralValue = "eu-west-1",
                UsageMode = NamespaceInputUsageMode.UseIfSelected
            });
            await db.SaveChangesAsync();
        }

        using var resolver = _scenario.Resolver();
        var resolved = await resolver.GetDeclared(module.Id, _scenario.OrganizationId);

        Assert.Contains(resolved.SelectedNamespaceParamsFromSecrets, s => s.InputName == "db_password");
        Assert.Contains(resolved.NamespaceParamFromLiterals!, l => l.Name == "region");
    }
}
