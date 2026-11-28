// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// An input is a param or an env var, and a name is unique within its kind on one owner. The kind
/// column is computed by the database from the row's type, whichever input type that is.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class InputKindTests(Fixture fixture) : IAsyncLifetime
{
    private NestedScenario _scenario = null!;
    private Namespace _namespace = null!;
    private Module _module = null!;

    public async Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        _namespace = await _scenario.AddNamespace("00");
        _module = await _scenario.AddModule(_namespace.Id);
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Fact]
    public async Task The_stored_kind_follows_the_type_of_the_row()
    {
        await Add(
            NamespaceInput<NamespaceParamFromLiteral>("A"),
            NamespaceInput<NamespaceEnvVarFromLiteral>("B"),
            NamespaceInput<NamespaceEnvVarUnset>("C"),
            ModuleInput<ModuleParamFromLiteral>("A"),
            ModuleInput<ModuleEnvVarFromLiteral>("B"),
            ModuleInput<ModuleEnvVarUnset>("C"));

        Assert.Equal(["A=Param", "B=EnvVar", "C=EnvVar"], await Stored("NamespaceInputs", "NamespaceId", _namespace.Id));
        Assert.Equal(["A=Param", "B=EnvVar", "C=EnvVar"], await Stored("ModuleInputs", "ModuleId", _module.Id));
    }

    [Fact]
    public async Task A_param_and_an_env_var_of_one_name_can_sit_on_the_same_namespace()
    {
        await Add(NamespaceInput<NamespaceParamFromLiteral>("REGION"), NamespaceInput<NamespaceEnvVarFromLiteral>("REGION"));
    }

    [Fact]
    public async Task A_param_and_an_env_var_of_one_name_can_sit_on_the_same_module()
    {
        await Add(ModuleInput<ModuleParamFromLiteral>("REGION"), ModuleInput<ModuleEnvVarFromLiteral>("REGION"));
    }

    [Fact]
    public async Task Two_params_of_one_name_cannot_sit_on_the_same_namespace_whatever_their_types()
    {
        await Add(NamespaceInput<NamespaceParamFromLiteral>("REGION"));

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => Add(NamespaceInput<NamespaceParamUnset>("REGION")));
    }

    [Fact]
    public async Task Two_env_vars_of_one_name_cannot_sit_on_the_same_module_whatever_their_types()
    {
        await Add(ModuleInput<ModuleEnvVarFromLiteral>("REGION"));

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => Add(ModuleInput<ModuleEnvVarUnset>("REGION")));
    }

    private T NamespaceInput<T>(string name) where T : NamespaceInput, new()
    {
        var input = new T { Id = Guid.NewGuid(), Name = name, NamespaceId = _namespace.Id, OrganizationId = _scenario.OrganizationId };
        if (input is NamespaceInputFromLiteral literal) literal.LiteralValue = "value";
        return input;
    }

    private T ModuleInput<T>(string name) where T : ModuleInput, new()
    {
        var input = new T { Id = Guid.NewGuid(), Name = name, ModuleId = _module.Id, OrganizationId = _scenario.OrganizationId };
        if (input is ModuleInputFromLiteral literal) literal.LiteralValue = "value";
        return input;
    }

    private async Task Add(params object[] inputs)
    {
        await using var db = fixture.CreateDbContext();
        db.AddRange(inputs);
        await db.SaveChangesAsync();
    }

    private async Task<List<string>> Stored(string table, string ownerColumn, Guid ownerId)
    {
        await using var db = fixture.CreateDbContext();
        return await db.Database
            .SqlQueryRaw<string>($"SELECT Name + '=' + InputKind AS Value FROM {table} WHERE {ownerColumn} = {{0}} ORDER BY Name", ownerId)
            .ToListAsync();
    }

    [Fact]
    public async Task A_secret_is_stored_with_the_scope_of_its_type()
    {
        await using var db = fixture.CreateDbContext();

        var stored = await db.Database
            .SqlQueryRaw<string>("SELECT DISTINCT Discriminator + '|' + ScopeKind AS Value FROM dbo.Secrets")
            .ToListAsync();

        Assert.Equal(
            ["ModuleSecret|Module", "NamespaceSecret|Namespace", "StackSecret|Stack"],
            stored.Order().ToList());
    }
}
