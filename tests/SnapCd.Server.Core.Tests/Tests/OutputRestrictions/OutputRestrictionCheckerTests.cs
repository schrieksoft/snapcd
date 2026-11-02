// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.OutputSupplies;
using SnapCd.Server.Core.Entities.Definition.Outputs;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Nonsecured.ParamResolver.Helpers;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.OutputRestrictions;

/// <summary>
/// Which of a producing Module's Outputs a consumer may read. The producer withholds its whole
/// Output Set through Module.OutputSetDownstreamRestriction, or single Outputs by name through
/// ModuleOutputDownstreamRestriction.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class OutputRestrictionCheckerTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private Guid _producerId;
    private Guid _consumerId;
    private Guid _organizationId;
    private Guid _outputSetId;
    private readonly List<Guid> _outputs = [];
    private readonly List<Guid> _outputSets = [];
    private readonly List<Guid> _restrictions = [];
    private readonly List<Guid> _supplies = [];
    private readonly List<Guid> _outputSupplies = [];

    public OutputRestrictionCheckerTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _producerId = _fixture.Modules["0001"].Id;
        _consumerId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;

        _outputSetId = await AddOutputSet(_producerId);
        await AddLiteralOutput(_outputSetId, "vpc_id");
        await AddSecretOutput(_outputSetId, "db_password");

        await ClearSuppliesFromProducer();
    }

    /// <summary>
    /// A supply overrides every restriction, so a row left behind by another test class reading
    /// from this producer makes every restriction assertion here pass for the wrong reason.
    /// </summary>
    private async Task ClearSuppliesFromProducer()
    {
        await using var db = _fixture.CreateDbContext();
        await db.OutputSetModuleSupplies.Where(x => x.ModuleId == _producerId).ExecuteDeleteAsync();
        await db.OutputSetNamespaceSupplies.Where(x => x.ModuleId == _producerId).ExecuteDeleteAsync();
        await db.OutputModuleSupplies.Where(x => x.ModuleId == _producerId).ExecuteDeleteAsync();
        await db.OutputNamespaceSupplies.Where(x => x.ModuleId == _producerId).ExecuteDeleteAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();

        if (_restrictions.Count > 0)
            await db.ModuleOutputDownstreamRestrictions
                .Where(r => _restrictions.Contains(r.Id)).ExecuteDeleteAsync();

        if (_supplies.Count > 0)
        {
            await db.OutputSetModuleSupplies.Where(x => _supplies.Contains(x.Id)).ExecuteDeleteAsync();
            await db.OutputSetNamespaceSupplies.Where(x => _supplies.Contains(x.Id)).ExecuteDeleteAsync();
        }

        if (_outputSupplies.Count > 0)
        {
            await db.OutputModuleSupplies.Where(x => _outputSupplies.Contains(x.Id)).ExecuteDeleteAsync();
            await db.OutputNamespaceSupplies.Where(x => _outputSupplies.Contains(x.Id)).ExecuteDeleteAsync();
        }

        if (_outputs.Count > 0)
            await db.Outputs.Where(o => _outputs.Contains(o.Id)).ExecuteDeleteAsync();

        if (_outputSets.Count > 0)
            await db.OutputSets.Where(s => _outputSets.Contains(s.Id)).ExecuteDeleteAsync();

        await ClearSetRestriction();
        await _dbContext.DisposeAsync();
    }

    // Unrestricted: the behaviour everything else is measured against.

    [Fact]
    public async Task An_unrestricted_producer_supplies_every_output()
    {
        var readable = await Checker().FilterReadable(await ProducerOutputs(), _consumerId, _organizationId);

        Assert.Equal(2, readable.Count);
    }

    [Fact]
    public async Task An_unrestricted_output_is_not_refused_by_name()
    {
        await Checker().EnsureReadable(_consumerId, [(_producerId, "vpc_id")], _organizationId);
    }

    // All: nothing leaves the producer.

    [Fact]
    public async Task All_withholds_every_output_from_an_output_set_read()
    {
        await SetRestriction(OutputSetDownstreamRestriction.All);

        var readable = await Checker().FilterReadable(await ProducerOutputs(), _consumerId, _organizationId);

        Assert.Empty(readable);
    }

    [Fact]
    public async Task All_refuses_a_named_output()
    {
        await SetRestriction(OutputSetDownstreamRestriction.All);

        var ex = await Assert.ThrowsAsync<OutputRestrictedException>(
            () => Checker().EnsureReadable(_consumerId, [(_producerId, "vpc_id")], _organizationId));

        Assert.Contains("vpc_id", ex.Message);
        Assert.Contains("whole Output Set", ex.Message);
    }

    // SensitiveOnly: the SecretOutput rows are withheld, the LiteralOutput rows are not.

    [Fact]
    public async Task SensitiveOnly_withholds_the_secret_output_and_passes_the_literal_one()
    {
        await SetRestriction(OutputSetDownstreamRestriction.SensitiveOnly);

        var readable = await Checker().FilterReadable(await ProducerOutputs(), _consumerId, _organizationId);

        var output = Assert.Single(readable);
        Assert.Equal("vpc_id", output.Name);
    }

    [Fact]
    public async Task SensitiveOnly_refuses_a_named_secret_output()
    {
        await SetRestriction(OutputSetDownstreamRestriction.SensitiveOnly);

        var ex = await Assert.ThrowsAsync<OutputRestrictedException>(
            () => Checker().EnsureReadable(_consumerId, [(_producerId, "db_password")], _organizationId));

        Assert.Contains("db_password", ex.Message);
        Assert.Contains("sensitive", ex.Message);
    }

    [Fact]
    public async Task SensitiveOnly_allows_a_named_literal_output()
    {
        await SetRestriction(OutputSetDownstreamRestriction.SensitiveOnly);

        await Checker().EnsureReadable(_consumerId, [(_producerId, "vpc_id")], _organizationId);
    }

    // A single Output restricted by name, whatever its sensitivity.

    [Fact]
    public async Task A_named_restriction_withholds_that_output_from_an_output_set_read()
    {
        await AddNamedRestriction("vpc_id");

        var readable = await Checker().FilterReadable(await ProducerOutputs(), _consumerId, _organizationId);

        var output = Assert.Single(readable);
        Assert.Equal("db_password", output.Name);
    }

    [Fact]
    public async Task A_named_restriction_refuses_that_output_by_name()
    {
        await AddNamedRestriction("vpc_id");

        var ex = await Assert.ThrowsAsync<OutputRestrictedException>(
            () => Checker().EnsureReadable(_consumerId, [(_producerId, "vpc_id")], _organizationId));

        Assert.Contains("vpc_id", ex.Message);
    }

    [Fact]
    public async Task A_named_restriction_leaves_the_other_outputs_alone()
    {
        await AddNamedRestriction("vpc_id");

        await Checker().EnsureReadable(_consumerId, [(_producerId, "db_password")], _organizationId);
    }

    /// <summary>
    /// A named restriction applies to a literal Output as readily as a secret one. SensitiveOnly is
    /// the only restriction that reads the Output's type.
    /// </summary>
    [Fact]
    public async Task A_named_restriction_applies_to_a_literal_output()
    {
        await AddNamedRestriction("vpc_id");

        var readable = await Checker().FilterReadable(await ProducerOutputs(), _consumerId, _organizationId);

        Assert.DoesNotContain(readable, o => o.Name == "vpc_id");
    }

    /// <summary>The message has to name what was refused and who withheld it, or it sends the
    /// reader looking for a missing variable instead of a restriction.</summary>
    [Fact]
    public async Task The_refusal_names_the_consumer_the_producer_and_the_output()
    {
        await SetRestriction(OutputSetDownstreamRestriction.All);

        var ex = await Assert.ThrowsAsync<OutputRestrictedException>(
            () => Checker().EnsureReadable(_consumerId, [(_producerId, "vpc_id")], _organizationId));

        Assert.Contains(_fixture.Modules["0000"].Name, ex.Message);
        Assert.Contains(_fixture.Modules["0001"].Name, ex.Message);
        Assert.Contains("vpc_id", ex.Message);
    }

    [Fact]
    public async Task Several_refusals_are_reported_together()
    {
        await SetRestriction(OutputSetDownstreamRestriction.All);

        var ex = await Assert.ThrowsAsync<OutputRestrictedException>(
            () => Checker().EnsureReadable(
                _consumerId,
                [(_producerId, "vpc_id"), (_producerId, "db_password")],
                _organizationId));

        Assert.Contains("vpc_id", ex.Message);
        Assert.Contains("db_password", ex.Message);
    }

    /// <summary>A restriction belongs to its producer, so another Module's Outputs are untouched.</summary>
    [Fact]
    public async Task A_restriction_on_one_producer_does_not_reach_another()
    {
        await SetRestriction(OutputSetDownstreamRestriction.All);

        await Checker().EnsureReadable(
            _consumerId,
            [(_fixture.Modules["0002"].Id, "vpc_id")],
            _organizationId);
    }

    // Supply overrides restriction, which is the rule the feature rests on.

    [Fact]
    public async Task A_module_supply_overrides_an_All_restriction()
    {
        await SetRestriction(OutputSetDownstreamRestriction.All);
        await AddModuleSupply();

        var readable = await Checker().FilterReadable(await ProducerOutputs(), _consumerId, _organizationId);

        Assert.Equal(2, readable.Count);
    }

    [Fact]
    public async Task A_module_supply_overrides_a_named_restriction()
    {
        await AddNamedRestriction("vpc_id");
        await AddModuleSupply();

        await Checker().EnsureReadable(_consumerId, [(_producerId, "vpc_id")], _organizationId);
    }

    [Fact]
    public async Task A_module_supply_delivers_even_a_sensitive_output()
    {
        await SetRestriction(OutputSetDownstreamRestriction.SensitiveOnly);
        await AddModuleSupply();

        await Checker().EnsureReadable(_consumerId, [(_producerId, "db_password")], _organizationId);
    }

    [Fact]
    public async Task A_namespace_supply_reaches_a_module_in_that_namespace()
    {
        await SetRestriction(OutputSetDownstreamRestriction.All);
        await AddNamespaceSupply();

        var readable = await Checker().FilterReadable(await ProducerOutputs(), _consumerId, _organizationId);

        Assert.Equal(2, readable.Count);
    }

    /// <summary>A supply names one consumer, so it must not open the Output Set to the rest.</summary>
    [Fact]
    public async Task A_module_supply_does_not_reach_a_module_it_does_not_name()
    {
        await SetRestriction(OutputSetDownstreamRestriction.All);
        await AddModuleSupply();

        var other = _fixture.Modules["0003"].Id;
        await Assert.ThrowsAsync<OutputRestrictedException>(
            () => Checker().EnsureReadable(other, [(_producerId, "vpc_id")], _organizationId));
    }

    [Fact]
    public async Task A_namespace_supply_does_not_reach_a_module_in_another_namespace()
    {
        await SetRestriction(OutputSetDownstreamRestriction.All);
        await AddNamespaceSupply();

        var elsewhere = _fixture.Modules["0010"].Id;
        await Assert.ThrowsAsync<OutputRestrictedException>(
            () => Checker().EnsureReadable(elsewhere, [(_producerId, "vpc_id")], _organizationId));
    }

    // A single Output supplied by name, not the whole Output Set.

    [Fact]
    public async Task An_output_supply_overrides_an_All_restriction_for_that_output_only()
    {
        await SetRestriction(OutputSetDownstreamRestriction.All);
        await AddOutputModuleSupply("vpc_id");

        var readable = await Checker().FilterReadable(await ProducerOutputs(), _consumerId, _organizationId);

        var output = Assert.Single(readable);
        Assert.Equal("vpc_id", output.Name);
    }

    [Fact]
    public async Task An_output_supply_allows_that_output_by_name()
    {
        await SetRestriction(OutputSetDownstreamRestriction.All);
        await AddOutputModuleSupply("vpc_id");

        await Checker().EnsureReadable(_consumerId, [(_producerId, "vpc_id")], _organizationId);
    }

    [Fact]
    public async Task An_output_supply_does_not_allow_a_different_output()
    {
        await SetRestriction(OutputSetDownstreamRestriction.All);
        await AddOutputModuleSupply("vpc_id");

        await Assert.ThrowsAsync<OutputRestrictedException>(
            () => Checker().EnsureReadable(_consumerId, [(_producerId, "db_password")], _organizationId));
    }

    [Fact]
    public async Task An_output_supply_overrides_a_named_restriction_on_that_output()
    {
        await AddNamedRestriction("vpc_id");
        await AddOutputModuleSupply("vpc_id");

        await Checker().EnsureReadable(_consumerId, [(_producerId, "vpc_id")], _organizationId);
    }

    [Fact]
    public async Task An_output_namespace_supply_reaches_a_module_in_that_namespace()
    {
        await SetRestriction(OutputSetDownstreamRestriction.All);
        await AddOutputNamespaceSupply("vpc_id");

        await Checker().EnsureReadable(_consumerId, [(_producerId, "vpc_id")], _organizationId);
    }

    [Fact]
    public async Task An_output_supply_does_not_reach_a_module_it_does_not_name()
    {
        await SetRestriction(OutputSetDownstreamRestriction.All);
        await AddOutputModuleSupply("vpc_id");

        var other = _fixture.Modules["0003"].Id;
        await Assert.ThrowsAsync<OutputRestrictedException>(
            () => Checker().EnsureReadable(other, [(_producerId, "vpc_id")], _organizationId));
    }

    private async Task AddOutputModuleSupply(string outputName)
    {
        var supply = new OutputModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ModuleId = _producerId,
            ConsumingModuleId = _consumerId,
            OutputName = outputName
        };

        await using var db = _fixture.CreateDbContext();
        db.OutputModuleSupplies.Add(supply);
        await db.SaveChangesAsync();

        _outputSupplies.Add(supply.Id);
    }

    private async Task AddOutputNamespaceSupply(string outputName)
    {
        var supply = new OutputNamespaceSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ModuleId = _producerId,
            ConsumingNamespaceId = _fixture.Namespaces["000"].Id,
            OutputName = outputName
        };

        await using var db = _fixture.CreateDbContext();
        db.OutputNamespaceSupplies.Add(supply);
        await db.SaveChangesAsync();

        _outputSupplies.Add(supply.Id);
    }

    private async Task AddModuleSupply()
    {
        var supply = new OutputSetModuleSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ModuleId = _producerId,
            ConsumingModuleId = _consumerId
        };

        await using var db = _fixture.CreateDbContext();
        db.OutputSetModuleSupplies.Add(supply);
        await db.SaveChangesAsync();

        _supplies.Add(supply.Id);
    }

    private async Task AddNamespaceSupply()
    {
        var supply = new OutputSetNamespaceSupply
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ModuleId = _producerId,
            ConsumingNamespaceId = _fixture.Namespaces["000"].Id
        };

        await using var db = _fixture.CreateDbContext();
        db.OutputSetNamespaceSupplies.Add(supply);
        await db.SaveChangesAsync();

        _supplies.Add(supply.Id);
    }

    private OutputRestrictionChecker Checker() => new(_fixture.CreateDbContext());

    private async Task<List<Output>> ProducerOutputs()
    {
        await using var db = _fixture.CreateDbContext();
        return await db.Outputs.Where(o => o.OutputSetId == _outputSetId).ToListAsync();
    }

    private async Task<Guid> AddOutputSet(Guid moduleId)
    {
        var set = new OutputSet
        {
            Id = Guid.NewGuid(),
            ModuleId = moduleId,
            OrganizationId = _organizationId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Checksum = $"restriction-tests-{Guid.NewGuid():N}"
        };

        await using var db = _fixture.CreateDbContext();
        db.OutputSets.Add(set);
        await db.SaveChangesAsync();

        _outputSets.Add(set.Id);
        return set.Id;
    }

    private Task AddLiteralOutput(Guid outputSetId, string name) =>
        AddOutput(new LiteralOutput { Name = name, Type = "string", Value = "test-value" }, outputSetId);

    private Task AddSecretOutput(Guid outputSetId, string name) =>
        AddOutput(new SecretOutput { Name = name, Type = "string", RemoteSecretName = $"{name}-secret" }, outputSetId);

    private async Task AddOutput(Output output, Guid outputSetId)
    {
        output.Id = Guid.NewGuid();
        output.OutputSetId = outputSetId;
        output.OrganizationId = _organizationId;

        await using var db = _fixture.CreateDbContext();
        db.Outputs.Add(output);
        await db.SaveChangesAsync();

        _outputs.Add(output.Id);
    }

    private async Task AddNamedRestriction(string outputName)
    {
        var restriction = new ModuleOutputDownstreamRestriction
        {
            Id = Guid.NewGuid(),
            ModuleId = _producerId,
            OrganizationId = _organizationId,
            OutputName = outputName
        };

        await using var db = _fixture.CreateDbContext();
        db.ModuleOutputDownstreamRestrictions.Add(restriction);
        await db.SaveChangesAsync();

        _restrictions.Add(restriction.Id);
    }

    private async Task SetRestriction(OutputSetDownstreamRestriction? restriction)
    {
        await using var db = _fixture.CreateDbContext();
        await db.Modules
            .Where(m => m.Id == _producerId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.OutputSetDownstreamRestriction, restriction));
    }

    private Task ClearSetRestriction() => SetRestriction(null);
}
