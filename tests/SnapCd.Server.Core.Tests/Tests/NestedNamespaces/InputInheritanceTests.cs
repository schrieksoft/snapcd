// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Services.Utils.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// Every input declared on a Namespace reaches every Module in it and below it, under its own
/// name. The nearest declaration of a name wins. An input of the Unset type carries no value and
/// drops whatever of that name would have been inherited: for one Module, or for a Namespace and
/// all below it.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class InputInheritanceTests(Fixture fixture) : IAsyncLifetime
{
    private NestedScenario _scenario = null!;
    private Namespace _root = null!;
    private Namespace _middle = null!;
    private Namespace _leaf = null!;

    public async Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        _root = await _scenario.AddNamespace("00");
        _middle = await _scenario.AddNamespace("00", _root.Id);
        _leaf = await _scenario.AddNamespace("00", _middle.Id);
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Theory]
    [InlineData(NamespaceInputUsageMode.UseByDefault)]
    [InlineData(NamespaceInputUsageMode.UseIfSelected)]
    public async Task An_input_on_a_namespace_reaches_a_module_two_namespaces_down_whatever_its_usage_mode(NamespaceInputUsageMode mode)
    {
        await Literal(_root, "region", "eu-west-1", usageMode: mode);
        var module = await _scenario.AddModule(_leaf.Id);

        var resolved = await Resolve(module);

        Assert.Equal("eu-west-1", ParamValue(resolved, "region"));
    }

    [Fact]
    public async Task The_nearest_namespace_that_declares_a_name_supplies_it()
    {
        await Literal(_root, "region", "eu-west-1");
        await Literal(_middle, "region", "us-east-1");
        var module = await _scenario.AddModule(_leaf.Id);

        Assert.Equal("us-east-1", ParamValue(await Resolve(module), "region"));
    }

    [Fact]
    public async Task A_nearer_literal_replaces_a_secret_backed_input_of_the_same_name()
    {
        await Secret(_root, "db_password");
        await Literal(_middle, "db_password", "not-a-secret");
        var module = await _scenario.AddModule(_leaf.Id);

        var resolved = await Resolve(module);

        Assert.Equal("not-a-secret", ParamValue(resolved, "db_password"));
        Assert.DoesNotContain(resolved.SelectedNamespaceParamsFromSecrets, s => s.InputName == "db_password");
    }

    [Fact]
    public async Task A_name_unset_on_a_namespace_reaches_nothing_in_it_or_below_it()
    {
        await Literal(_root, "region", "eu-west-1");
        await Unset(_middle, "region");
        var above = await _scenario.AddModule(_root.Id);
        var inIt = await _scenario.AddModule(_middle.Id);
        var below = await _scenario.AddModule(_leaf.Id);

        Assert.Equal("eu-west-1", ParamValue(await Resolve(above), "region"));
        Assert.Null(ParamValue(await Resolve(inIt), "region"));
        Assert.Null(ParamValue(await Resolve(below), "region"));
    }

    [Fact]
    public async Task A_name_unset_on_a_namespace_can_be_set_again_below_it()
    {
        await Literal(_root, "region", "eu-west-1");
        await Unset(_middle, "region");
        await Literal(_leaf, "region", "ap-south-1");
        var module = await _scenario.AddModule(_leaf.Id);

        Assert.Equal("ap-south-1", ParamValue(await Resolve(module), "region"));
    }

    [Fact]
    public async Task A_secret_backed_input_unset_below_is_not_read_for_the_modules_it_no_longer_reaches()
    {
        await Secret(_root, "db_password");
        await Unset(_middle, "db_password");
        var module = await _scenario.AddModule(_leaf.Id);

        Assert.DoesNotContain((await Resolve(module)).SelectedNamespaceParamsFromSecrets, s => s.InputName == "db_password");
    }

    [Fact]
    public async Task A_name_unset_on_a_module_reaches_that_module_only()
    {
        await Literal(_root, "region", "eu-west-1");
        var declines = await _scenario.AddModule(_leaf.Id);
        var takesIt = await _scenario.AddModule(_leaf.Id);

        await using (var db = fixture.CreateDbContext())
        {
            db.ModuleInputs.Add(new ModuleParamUnset
            {
                Id = Guid.NewGuid(), Name = "region", ModuleId = declines.Id, OrganizationId = _scenario.OrganizationId
            });
            await db.SaveChangesAsync();
        }

        var resolved = await Resolve(declines);

        Assert.Null(ParamValue(resolved, "region"));
        Assert.Equal("eu-west-1", ParamValue(await Resolve(takesIt), "region"));
    }

    [Fact]
    public async Task A_param_and_an_env_var_of_the_same_name_are_unset_separately()
    {
        await Literal(_root, "REGION", "eu-west-1", envVar: true);
        await Literal(_middle, "REGION", "eu-west-1");
        await Unset(_leaf, "REGION");
        var module = await _scenario.AddModule(_leaf.Id);

        var resolved = await Resolve(module);

        Assert.Null(ParamValue(resolved, "REGION"));
        Assert.Contains(resolved.NamespaceEnvVarFromLiterals!, l => l.Name == "REGION");
    }

    private static string? ParamValue(ResolvedModule resolved, string name) =>
        resolved.NamespaceParamFromLiterals!.SingleOrDefault(l => l.Name == name)?.LiteralValue;

    private async Task<ResolvedModule> Resolve(Module module)
    {
        using var resolver = _scenario.Resolver();
        return await resolver.GetDeclared(module.Id, _scenario.OrganizationId);
    }

    private async Task Literal(
        Namespace ns, string name, string value,
        bool envVar = false,
        NamespaceInputUsageMode usageMode = NamespaceInputUsageMode.UseByDefault)
    {
        NamespaceInputFromLiteral input = envVar ? new NamespaceEnvVarFromLiteral() : new NamespaceParamFromLiteral();
        input.Id = Guid.NewGuid();
        input.Name = name;
        input.NamespaceId = ns.Id;
        input.OrganizationId = _scenario.OrganizationId;
        input.LiteralValue = value;
        input.UsageMode = usageMode;

        await using var db = fixture.CreateDbContext();
        db.NamespaceInputs.Add(input);
        await db.SaveChangesAsync();
    }

    private async Task Unset(Namespace ns, string name)
    {
        await using var db = fixture.CreateDbContext();
        db.NamespaceInputs.Add(new NamespaceParamUnset
        {
            Id = Guid.NewGuid(), Name = name, NamespaceId = ns.Id, OrganizationId = _scenario.OrganizationId
        });
        await db.SaveChangesAsync();
    }

    private async Task Secret(Namespace ns, string name)
    {
        await using var db = fixture.CreateDbContext();
        var secret = fixture.CreateTestNamespaceSecret($"nested_{Guid.NewGuid():N}", ns.Id, _scenario.OrganizationId, db);

        db.NamespaceInputs.Add(new NamespaceParamFromSecret
        {
            Id = Guid.NewGuid(), Name = name, NamespaceId = ns.Id, SecretId = secret.Id,
            OrganizationId = _scenario.OrganizationId, UsageMode = NamespaceInputUsageMode.UseByDefault
        });
        await db.SaveChangesAsync();
    }
}
