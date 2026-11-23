// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Services.Nonsecured.Approvals;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// A pin on a Namespace binds everything below it, a Module included. The Module may hold its own
/// value; resolution does not use it.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class OverridePinTests(Fixture fixture) : IAsyncLifetime
{
    private NestedScenario _scenario = null!;

    public Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Fact]
    public async Task A_module_cannot_override_a_setting_an_ancestor_forbids_overriding()
    {
        var parent = await _scenario.AddNamespace("00", configure: n =>
        {
            n.DefaultApprovalTimeoutMinutes = 30;
            n.OverrideOnApprovalTimeoutMinutes = OverridePolicy.Forbidden;
        });
        var child = await _scenario.AddNamespace("00", parent.Id);
        var module = await _scenario.AddModule(child.Id, m => m.ApprovalTimeoutMinutes = 600);

        using var resolver = _scenario.Resolver();
        var resolved = await resolver.GetDeclared(module.Id, _scenario.OrganizationId);

        Assert.Equal(30, resolved.ApprovalTimeoutMinutes);
    }

    [Fact]
    public async Task A_module_cannot_loosen_a_setting_an_ancestor_allows_only_to_be_tightened()
    {
        var parent = await _scenario.AddNamespace("00", configure: n =>
        {
            n.DefaultCleanInitEnabled = true;
            n.OverrideOnCleanInitEnabled = OrderedOverridePolicy.OnlyStricter;
        });
        var child = await _scenario.AddNamespace("00", parent.Id);
        var module = await _scenario.AddModule(child.Id, m => m.CleanInitEnabled = false);

        using var resolver = _scenario.Resolver();
        var resolved = await resolver.GetDeclared(module.Id, _scenario.OrganizationId);

        Assert.True(resolved.CleanInitEnabled);
    }

    [Fact]
    public async Task A_module_cannot_lower_an_approval_threshold_an_ancestor_forbids_overriding()
    {
        var parent = await _scenario.AddNamespace("00", configure: n =>
        {
            n.DefaultStateMigrationApprovalThreshold = 2;
            n.OverrideOnStateMigrationApprovalThreshold = OrderedOverridePolicy.Forbidden;
        });
        var child = await _scenario.AddNamespace("00", parent.Id);
        var module = await _scenario.AddModule(child.Id, m => m.StateMigrationApprovalThreshold = 0);

        await using var db = fixture.CreateDbContext();
        var threshold = await StateMigrationApprovalThreshold.Resolve(module.Id, _scenario.OrganizationId, db);

        Assert.Equal(2, threshold);
    }

    [Fact]
    public async Task A_module_cannot_loosen_an_output_restriction_an_ancestor_forbids_overriding()
    {
        var parent = await _scenario.AddNamespace("00", configure: n =>
        {
            n.DefaultOutputSetDownstreamRestriction = OutputSetDownstreamRestriction.All;
            n.OverrideOnOutputSetDownstreamRestriction = OrderedOverridePolicy.Forbidden;
        });
        var child = await _scenario.AddNamespace("00", parent.Id);
        var producer = await _scenario.AddModule(
            child.Id, m => m.OutputSetDownstreamRestriction = OutputSetDownstreamRestriction.SensitiveOnly);
        var outputs = await _scenario.AddOutputs(producer.Id);

        var elsewhere = await _scenario.AddNamespace("00");
        var consumer = await _scenario.AddModule(elsewhere.Id);

        var readable = await _scenario.Checker().FilterReadable(outputs, consumer.Id, _scenario.OrganizationId);

        Assert.Empty(readable);
    }

    [Fact]
    public async Task A_module_may_tighten_an_output_restriction_an_ancestor_allows_only_to_be_tightened()
    {
        var parent = await _scenario.AddNamespace("00", configure: n =>
        {
            n.DefaultOutputSetDownstreamRestriction = OutputSetDownstreamRestriction.SensitiveOnly;
            n.OverrideOnOutputSetDownstreamRestriction = OrderedOverridePolicy.OnlyStricter;
        });
        var child = await _scenario.AddNamespace("00", parent.Id);
        var producer = await _scenario.AddModule(
            child.Id, m => m.OutputSetDownstreamRestriction = OutputSetDownstreamRestriction.All);
        var outputs = await _scenario.AddOutputs(producer.Id);

        var elsewhere = await _scenario.AddNamespace("00");
        var consumer = await _scenario.AddModule(elsewhere.Id);

        var readable = await _scenario.Checker().FilterReadable(outputs, consumer.Id, _scenario.OrganizationId);

        Assert.Empty(readable);
    }
}
