// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Services.Nonsecured.RippleProbes;
using SnapCd.Server.Core.Services.Utils.RippleProbes;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.RippleProbes;

/// <summary>
/// The reverse walk: given a producer and the outputs its plan says will change, who consumes
/// them and under which of their own variable names.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class RippleProbeConsumerLookupTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private Guid _producerId;
    private Guid _consumerId;
    private Guid _consumerWithoutVariableSetId;
    private Guid _otherStackConsumerId;
    private Guid _organizationId;
    private readonly List<Guid> _inputs = [];
    private readonly List<Guid> _variableSets = [];

    public RippleProbeConsumerLookupTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _producerId = _fixture.Modules["0001"].Id;
        _consumerId = _fixture.Modules["0000"].Id;

        // The fixture gives Module0000 two VariableSets declaring unrelated names, so any test
        // about the unfiltered path needs a Module it has left without one.
        _consumerWithoutVariableSetId = _fixture.Modules["0003"].Id;

        _otherStackConsumerId = _fixture.Modules["0100"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_inputs.Count > 0)
            await _dbContext.ModuleInputs.Where(i => _inputs.Contains(i.Id)).ExecuteDeleteAsync();

        if (_variableSets.Count > 0)
            await _dbContext.VariableSets.Where(v => _variableSets.Contains(v.Id)).ExecuteDeleteAsync();

        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task A_param_from_output_is_found_and_renamed()
    {
        await AddParamFromOutput(_consumerId, outputName: "vpc_id", variableName: "network_id");

        var consumers = await Lookup(["vpc_id"]);

        var consumer = Assert.Single(consumers);
        Assert.Equal(_consumerId, consumer.ModuleId);
        Assert.Equal("network_id", consumer.VariableNamesByOutputName["vpc_id"]);
    }

    [Fact]
    public async Task An_envvar_from_output_is_found_and_renamed()
    {
        await AddEnvVarFromOutput(_consumerId, outputName: "vpc_id", variableName: "TF_VAR_network");

        var consumers = await Lookup(["vpc_id"]);

        var consumer = Assert.Single(consumers);
        Assert.Equal("TF_VAR_network", consumer.VariableNamesByOutputName["vpc_id"]);
    }

    /// <summary>
    /// An OutputSet edge names the producer alone, so every changed output reaches the consumer
    /// under its own name. There is no rename.
    /// </summary>
    [Fact]
    public async Task An_output_set_edge_takes_every_changed_output_under_its_own_name()
    {
        await AddParamFromOutputSet(_consumerWithoutVariableSetId);

        var consumers = await Lookup(["vpc_id", "subnet_id"]);

        var consumer = Assert.Single(consumers);
        Assert.Equal("vpc_id", consumer.VariableNamesByOutputName["vpc_id"]);
        Assert.Equal("subnet_id", consumer.VariableNamesByOutputName["subnet_id"]);
    }

    /// <summary>
    /// With a VariableSet present the set is filtered to what the consumer actually declares,
    /// matching OutputSetParamResolver.
    /// </summary>
    [Fact]
    public async Task An_output_set_edge_is_filtered_by_the_consumers_variable_set()
    {
        await AddParamFromOutputSet(_consumerId);
        await AddVariableSet(_consumerId, "vpc_id");

        var consumers = await Lookup(["vpc_id", "subnet_id"]);

        var consumer = Assert.Single(consumers);
        Assert.True(consumer.VariableNamesByOutputName.ContainsKey("vpc_id"));
        Assert.False(consumer.VariableNamesByOutputName.ContainsKey("subnet_id"),
            "subnet_id is not declared by the consumer, so it does not reach it");
    }

    /// <summary>
    /// A Module that has never run has no VariableSet, and must not be dropped: the trigger query
    /// treats that as "take everything" and so does this.
    /// </summary>
    [Fact]
    public async Task An_output_set_consumer_with_no_variable_set_is_still_found()
    {
        await AddParamFromOutputSet(_consumerWithoutVariableSetId);

        var consumers = await Lookup(["vpc_id"]);

        var consumer = Assert.Single(consumers);
        Assert.Equal(_consumerWithoutVariableSetId, consumer.ModuleId);
    }

    /// <summary>
    /// The inverse of the test above, and the one that proves the filter is really applied rather
    /// than the unfiltered path happening to be taken. Module0000 carries VariableSets from the
    /// fixture that declare neither of these outputs.
    /// </summary>
    [Fact]
    public async Task An_output_set_consumer_declaring_none_of_the_changed_outputs_is_not_found()
    {
        await AddParamFromOutputSet(_consumerId);

        Assert.Empty(await Lookup(["vpc_id", "subnet_id"]));
    }

    /// <summary>
    /// Only the latest VariableSet counts. Unioning every set would keep a variable a later set
    /// dropped, so an output no longer declared would still look like it reaches the consumer.
    /// </summary>
    [Fact]
    public async Task An_output_set_edge_ignores_variables_dropped_by_a_later_variable_set()
    {
        await AddParamFromOutputSet(_consumerWithoutVariableSetId);
        await AddVariableSet(_consumerWithoutVariableSetId, hoursAhead: 1, variableNames: ["vpc_id", "subnet_id"]);
        await AddVariableSet(_consumerWithoutVariableSetId, hoursAhead: 2, variableNames: ["vpc_id"]);

        var consumers = await Lookup(["vpc_id", "subnet_id"]);

        var consumer = Assert.Single(consumers);
        Assert.True(consumer.VariableNamesByOutputName.ContainsKey("vpc_id"));
        Assert.False(consumer.VariableNamesByOutputName.ContainsKey("subnet_id"),
            "subnet_id was dropped by the latest VariableSet, so it no longer reaches the consumer");
    }

    /// <summary>The case that catches a lookup returning everything.</summary>
    [Fact]
    public async Task A_consumer_of_a_different_output_is_not_found()
    {
        await AddParamFromOutput(_consumerId, outputName: "unrelated", variableName: "x");

        var consumers = await Lookup(["vpc_id"]);

        Assert.Empty(consumers);
    }

    [Fact]
    public async Task A_consumer_of_a_different_producer_is_not_found()
    {
        await AddParamFromOutput(_consumerId, outputName: "vpc_id", variableName: "network_id",
            outputModuleId: _fixture.Modules["0002"].Id);

        var consumers = await Lookup(["vpc_id"]);

        Assert.Empty(consumers);
    }

    /// <summary>
    /// Cross-stack references are filtered out rather than refused. OutputSetParamResolver throws
    /// on the same condition, because it is resolving inputs for a job that is about to run; this
    /// only answers which Modules are affected, and one in another stack is not.
    /// </summary>
    [Fact]
    public async Task A_consumer_in_another_stack_is_filtered_out()
    {
        await AddParamFromOutput(_otherStackConsumerId, outputName: "vpc_id", variableName: "network_id");

        var consumers = await Lookup(["vpc_id"]);

        Assert.Empty(consumers);
    }

    [Fact]
    public async Task Several_variables_of_one_consumer_collapse_into_one_entry()
    {
        await AddParamFromOutput(_consumerId, outputName: "vpc_id", variableName: "network_id");
        await AddEnvVarFromOutput(_consumerId, outputName: "subnet_id", variableName: "TF_VAR_subnet");

        var consumers = await Lookup(["vpc_id", "subnet_id"]);

        var consumer = Assert.Single(consumers);
        Assert.Equal(2, consumer.VariableNamesByOutputName.Count);
        Assert.Equal("network_id", consumer.VariableNamesByOutputName["vpc_id"]);
        Assert.Equal("TF_VAR_subnet", consumer.VariableNamesByOutputName["subnet_id"]);
    }

    [Fact]
    public async Task No_changed_outputs_means_no_consumers()
    {
        await AddParamFromOutput(_consumerId, outputName: "vpc_id", variableName: "network_id");

        Assert.Empty(await Lookup([]));
    }

    private async Task<List<RippleProbeConsumer>> Lookup(string[] changedOutputNames)
    {
        using var lookup = new RippleProbeConsumerLookup(_fixture.CreateDbContext());
        return await lookup.ListConsumers(_producerId, _organizationId, changedOutputNames);
    }

    private Task AddParamFromOutput(Guid moduleId, string outputName, string variableName, Guid? outputModuleId = null) =>
        Add(new ModuleParamFromOutput
        {
            Name = variableName,
            OutputName = outputName,
            OutputModuleId = outputModuleId ?? _producerId
        }, moduleId);

    private Task AddEnvVarFromOutput(Guid moduleId, string outputName, string variableName) =>
        Add(new ModuleEnvVarFromOutput
        {
            Name = variableName,
            OutputName = outputName,
            OutputModuleId = _producerId
        }, moduleId);

    private Task AddParamFromOutputSet(Guid moduleId) =>
        Add(new ModuleParamFromOutputSet
        {
            Name = "from-set",
            OutputModuleId = _producerId
        }, moduleId);

    private async Task Add(ModuleInput input, Guid moduleId)
    {
        input.Id = Guid.NewGuid();
        input.ModuleId = moduleId;
        input.OrganizationId = _organizationId;

        await using var db = _fixture.CreateDbContext();
        db.ModuleInputs.Add(input);
        await db.SaveChangesAsync();

        _inputs.Add(input.Id);
    }

    private Task AddVariableSet(Guid moduleId, params string[] variableNames) =>
        AddVariableSet(moduleId, hoursAhead: 1, variableNames: variableNames);

    private async Task AddVariableSet(Guid moduleId, int hoursAhead, string[] variableNames)
    {
        var setId = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.VariableSets.Add(new VariableSet
        {
            Id = setId,
            ModuleId = moduleId,
            OrganizationId = _organizationId,
            // Seconds, as the fixture uses, and ahead of its sets so ordering is unambiguous.
            Timestamp = DateTimeOffset.UtcNow.AddHours(hoursAhead).ToUnixTimeSeconds(),
            Checksum = "test",
            Variables = variableNames.Select(n => new Variable
            {
                Id = Guid.NewGuid(),
                OrganizationId = _organizationId,
                VariableSetId = setId,
                Name = n
            }).ToList()
        });
        await db.SaveChangesAsync();

        _variableSets.Add(setId);
    }
}
