// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Repositories;

/// <summary>
/// A type is stored as a short tag plus the cty JSON definition of a complex type, so neither the
/// tag's column nor a long description bounds what a module may declare.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class VariableAndOutputTypeStorageTests
{
    private readonly Fixture _fixture;

    public VariableAndOutputTypeStorageTests(Fixture fixture) => _fixture = fixture;

    private const string LongObjectType =
        """["object",{"enable_auto_scaling":"bool","enable_host_encryption":"bool","enable_node_public_ip":"bool","node_count":"number","tags":["map","string"],"vm_size":"string","zones":["list","string"]}]""";

    [Fact]
    public async Task A_variable_stores_a_long_type_definition_description_and_default()
    {
        var module = _fixture.Modules["0000"];
        var description = new string('d', 4000);
        var defaultJson = $"{{\"vm_size\":\"{new string('v', 500)}\"}}";

        await using var db = _fixture.CreateDbContext();

        var variableSet = new VariableSet
        {
            Id = Guid.NewGuid(),
            OrganizationId = module.OrganizationId,
            ModuleId = module.Id,
            Checksum = Guid.NewGuid().ToString("N")
        };
        var variable = new Variable
        {
            Id = Guid.NewGuid(),
            OrganizationId = module.OrganizationId,
            VariableSetId = variableSet.Id,
            Name = "cluster_settings",
            Type = "object",
            TypeDefinition = LongObjectType,
            Default = defaultJson,
            Description = description
        };

        db.Add(variableSet);
        db.Add(variable);
        await db.SaveChangesAsync();

        try
        {
            await using var reader = _fixture.CreateDbContext();
            var stored = await reader.Set<Variable>().AsNoTracking().SingleAsync(v => v.Id == variable.Id);

            Assert.Equal("object", stored.Type);
            Assert.Equal(LongObjectType, stored.TypeDefinition);
            Assert.Equal(defaultJson, stored.Default);
            Assert.Equal(description, stored.Description);
        }
        finally
        {
            await db.Set<Variable>().Where(v => v.Id == variable.Id).ExecuteDeleteAsync();
            await db.Set<VariableSet>().Where(s => s.Id == variableSet.Id).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task A_primitive_variable_type_stores_no_definition()
    {
        var module = _fixture.Modules["0000"];

        await using var db = _fixture.CreateDbContext();

        var variableSet = new VariableSet
        {
            Id = Guid.NewGuid(),
            OrganizationId = module.OrganizationId,
            ModuleId = module.Id,
            Checksum = Guid.NewGuid().ToString("N")
        };
        var variable = new Variable
        {
            Id = Guid.NewGuid(),
            OrganizationId = module.OrganizationId,
            VariableSetId = variableSet.Id,
            Name = "region",
            Type = "string"
        };

        db.Add(variableSet);
        db.Add(variable);
        await db.SaveChangesAsync();

        try
        {
            await using var reader = _fixture.CreateDbContext();
            var stored = await reader.Set<Variable>().AsNoTracking().SingleAsync(v => v.Id == variable.Id);

            Assert.Equal("string", stored.Type);
            Assert.Null(stored.TypeDefinition);
            Assert.Null(stored.Default);
        }
        finally
        {
            await db.Set<Variable>().Where(v => v.Id == variable.Id).ExecuteDeleteAsync();
            await db.Set<VariableSet>().Where(s => s.Id == variableSet.Id).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task An_output_stores_the_type_definition_alongside_the_tag()
    {
        var module = _fixture.Modules["0000"];

        await using var db = _fixture.CreateDbContext();

        var outputSet = new OutputSet
        {
            Id = Guid.NewGuid(),
            OrganizationId = module.OrganizationId,
            ModuleId = module.Id,
            Checksum = Guid.NewGuid().ToString("N")
        };
        var output = new LiteralOutput
        {
            Id = Guid.NewGuid(),
            OrganizationId = module.OrganizationId,
            OutputSetId = outputSet.Id,
            Name = "cluster",
            Type = "object",
            TypeDefinition = LongObjectType,
            Value = "{}"
        };

        db.Add(outputSet);
        db.Add(output);
        await db.SaveChangesAsync();

        try
        {
            await using var reader = _fixture.CreateDbContext();
            var stored = await reader.Set<Output>().AsNoTracking().SingleAsync(o => o.Id == output.Id);

            Assert.Equal("object", stored.Type);
            Assert.Equal(LongObjectType, stored.TypeDefinition);
        }
        finally
        {
            await db.Set<Output>().Where(o => o.Id == output.Id).ExecuteDeleteAsync();
            await db.Set<OutputSet>().Where(s => s.Id == outputSet.Id).ExecuteDeleteAsync();
        }
    }
}
