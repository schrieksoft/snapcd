// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// Each kind of change that alters what a Module resolves has to reach its stored row, and a
/// delete must not be refused by a row that points at what is being deleted.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ModuleResolvedSettingsMaintenanceTests(Fixture fixture) : IAsyncLifetime
{
    private NestedScenario _scenario = null!;
    private Guid _owner;

    public Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        _owner = fixture.OrganizationPrincipals["0"][OrganizationRole.Owner].DirectUser.Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Fact]
    public async Task A_new_module_gets_a_row()
    {
        var ns = await _scenario.AddNamespace("00", configure: n => n.DefaultApprovalTimeoutMinutes = 45);
        var module = await _scenario.AddModule(ns.Id);

        Assert.Equal(45, (await Row(module.Id)).ApprovalTimeoutMinutes);
    }

    [Fact]
    public async Task A_default_changed_two_namespaces_up_reaches_the_row()
    {
        var root = await _scenario.AddNamespace("00");
        var middle = await _scenario.AddNamespace("00", root.Id);
        var leaf = await _scenario.AddNamespace("00", middle.Id);
        var module = await _scenario.AddModule(leaf.Id);

        Assert.Null((await Row(module.Id)).ApprovalTimeoutMinutes);

        await using (var db = fixture.CreateDbContext())
            await db.Namespaces.Where(n => n.Id == root.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.DefaultApprovalTimeoutMinutes, 30));

        Assert.Equal(30, (await Row(module.Id)).ApprovalTimeoutMinutes);
    }

    [Fact]
    public async Task A_pin_added_above_takes_the_module_s_own_value_away()
    {
        var parent = await _scenario.AddNamespace("00", configure: n => n.DefaultApprovalTimeoutMinutes = 30);
        var module = await _scenario.AddModule(parent.Id, m => m.ApprovalTimeoutMinutes = 600);

        Assert.Equal(600, (await Row(module.Id)).ApprovalTimeoutMinutes);

        await using (var db = fixture.CreateDbContext())
            await db.Namespaces.Where(n => n.Id == parent.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.OverrideOnApprovalTimeoutMinutes, OverridePolicy.Forbidden));

        Assert.Equal(30, (await Row(module.Id)).ApprovalTimeoutMinutes);
    }

    [Fact]
    public async Task A_module_s_own_value_changed_reaches_the_row()
    {
        var ns = await _scenario.AddNamespace("00");
        var module = await _scenario.AddModule(ns.Id);

        await using (var db = fixture.CreateDbContext())
            await db.Modules.Where(m => m.Id == module.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.ApprovalTimeoutMinutes, 15));

        Assert.Equal(15, (await Row(module.Id)).ApprovalTimeoutMinutes);
    }

    [Fact]
    public async Task A_module_moved_to_another_namespace_resolves_from_the_new_chain()
    {
        var from = await _scenario.AddNamespace("00", configure: n => n.DefaultApprovalTimeoutMinutes = 10);
        var to = await _scenario.AddNamespace("00", configure: n => n.DefaultApprovalTimeoutMinutes = 20);
        var module = await _scenario.AddModule(from.Id);

        await using (var db = fixture.CreateDbContext())
            await db.Modules.Where(m => m.Id == module.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.NamespaceId, to.Id));

        Assert.Equal(20, (await Row(module.Id)).ApprovalTimeoutMinutes);
    }

    [Fact]
    public async Task A_namespace_moved_under_another_resolves_its_modules_from_the_new_chain()
    {
        var from = await _scenario.AddNamespace("00", configure: n => n.DefaultApprovalTimeoutMinutes = 10);
        var to = await _scenario.AddNamespace("00", configure: n => n.DefaultApprovalTimeoutMinutes = 20);
        var moved = await _scenario.AddNamespace("00", from.Id);
        var module = await _scenario.AddModule(moved.Id);

        Assert.Equal(10, (await Row(module.Id)).ApprovalTimeoutMinutes);

        var stored = await _scenario.Load(moved.Id);
        stored.ParentNamespaceId = to.Id;
        await _scenario.NamespaceRepositoryFor(_owner).Update(stored);

        Assert.Equal(20, (await Row(module.Id)).ApprovalTimeoutMinutes);
    }

    [Fact]
    public async Task An_exemption_records_the_namespace_it_was_set_on()
    {
        var root = await _scenario.AddNamespace(
            "00", configure: n => n.DefaultOutputRestrictionExemption = OutputRestrictionExemption.OwnNamespaceAndDescendants);
        var child = await _scenario.AddNamespace("00", root.Id);
        var inherits = await _scenario.AddModule(child.Id);
        var setsItsOwn = await _scenario.AddModule(child.Id, m => m.OutputRestrictionExemption = OutputRestrictionExemption.OwnNamespace);

        Assert.Equal(root.Id, (await Row(inherits.Id)).OutputRestrictionExemptionNamespaceId);
        Assert.Equal(child.Id, (await Row(setsItsOwn.Id)).OutputRestrictionExemptionNamespaceId);
    }

    [Fact]
    public async Task A_namespace_holding_a_module_whose_exemption_it_set_can_be_deleted()
    {
        var ns = await _scenario.AddNamespace(
            "00", configure: n => n.DefaultOutputRestrictionExemption = OutputRestrictionExemption.OwnNamespace);
        var module = await _scenario.AddModule(ns.Id);
        Assert.Equal(ns.Id, (await Row(module.Id)).OutputRestrictionExemptionNamespaceId);

        await _scenario.NamespaceRepositoryFor(_owner).Delete(ns.Id, _scenario.OrganizationId);

        await using var db = fixture.CreateDbContext();
        Assert.False(await db.ModuleResolvedSettings.AnyAsync(r => r.ModuleId == module.Id));
    }

    [Fact]
    public async Task A_stack_holding_such_a_namespace_can_be_deleted()
    {
        Guid stackId, moduleId;

        await using (var db = fixture.CreateDbContext())
        {
            var stack = fixture.CreateTestStack($"resolved_{Guid.NewGuid():N}", _scenario.OrganizationId, db);
            var root = new Namespace
            {
                Id = Guid.NewGuid(), Name = "root", StackId = stack.Id, OrganizationId = _scenario.OrganizationId,
                DefaultOutputRestrictionExemption = OutputRestrictionExemption.OwnNamespaceAndDescendants
            };
            var child = new Namespace
            {
                Id = Guid.NewGuid(), Name = "child", StackId = stack.Id, OrganizationId = _scenario.OrganizationId,
                ParentNamespaceId = root.Id
            };
            db.Namespaces.AddRange(root, child);
            await db.SaveChangesAsync();

            var module = fixture.CreateTestModule($"resolved_{Guid.NewGuid():N}", "000", db);
            module.NamespaceId = child.Id;
            await db.SaveChangesAsync();

            (stackId, moduleId) = (stack.Id, module.Id);
            Assert.Equal(root.Id, (await Row(moduleId)).OutputRestrictionExemptionNamespaceId);
        }

        var principal = fixture.CreatePrincipalProvider(_owner, PrincipalDiscriminator.User, _scenario.OrganizationId);
        var stacks = new StackSecuredRepository(
            new StackRepository(
                fixture.CreateDbContext(), principal, fixture.CreateMockBus(),
                Options.Create(new StackRepositorySettings())),
            principal);

        await stacks.Delete(stackId, _scenario.OrganizationId);

        await using var check = fixture.CreateDbContext();
        Assert.False(await check.ModuleResolvedSettings.AnyAsync(r => r.ModuleId == moduleId));
    }

    [Fact]
    public async Task An_update_that_changes_nothing_inheritable_writes_to_neither_table()
    {
        var root = await _scenario.AddNamespace("00", configure: n => n.DefaultApprovalTimeoutMinutes = 30);
        var child = await _scenario.AddNamespace("00", root.Id);
        var module = await _scenario.AddModule(child.Id, m => m.ApprovalTimeoutMinutes = 15);

        var closureBefore = await RowsWritten("NamespaceAncestors");
        var resolvedBefore = await RowsWritten("ModuleResolvedSettings");

        await using (var db = fixture.CreateDbContext())
        {
            // Unrelated columns, and inheritable ones set to the value they already hold.
            await db.Namespaces.Where(n => n.Id == child.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.Name, $"renamed_{Guid.NewGuid():N}")
                    .SetProperty(n => n.ParentNamespaceId, root.Id));
            await db.Namespaces.Where(n => n.Id == root.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.DefaultApprovalTimeoutMinutes, 30)
                    .SetProperty(n => n.OverrideOnApprovalTimeoutMinutes, (OverridePolicy?)null));
            await db.Modules.Where(m => m.Id == module.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.Name, $"renamed_{Guid.NewGuid():N}")
                    .SetProperty(m => m.ApprovalTimeoutMinutes, 15)
                    .SetProperty(m => m.NamespaceId, child.Id));
        }

        Assert.Equal(closureBefore, await RowsWritten("NamespaceAncestors"));
        Assert.Equal(resolvedBefore, await RowsWritten("ModuleResolvedSettings"));
    }

    [Fact]
    public async Task A_change_that_leaves_a_module_resolving_the_same_writes_no_row()
    {
        var root = await _scenario.AddNamespace("00");
        var child = await _scenario.AddNamespace("00", root.Id);
        await _scenario.AddModule(child.Id, m => m.ApprovalTimeoutMinutes = 15);

        var resolvedBefore = await RowsWritten("ModuleResolvedSettings");

        // The Module sets its own value and nothing pins it, so a default above changes nothing for it.
        await using (var db = fixture.CreateDbContext())
            await db.Namespaces.Where(n => n.Id == root.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.DefaultApprovalTimeoutMinutes, 30));

        Assert.Equal(resolvedBefore, await RowsWritten("ModuleResolvedSettings"));
    }

    [Fact]
    public async Task Creating_a_namespace_writes_its_own_ancestry_rows_and_nothing_else()
    {
        var root = await _scenario.AddNamespace("00");
        var child = await _scenario.AddNamespace("00", root.Id);
        await _scenario.AddModule(child.Id);

        var closureBefore = await RowsWritten("NamespaceAncestors");
        var resolvedBefore = await RowsWritten("ModuleResolvedSettings");

        await _scenario.AddNamespace("00", child.Id);

        // Itself, its parent and its grandparent.
        Assert.Equal(closureBefore + 3, await RowsWritten("NamespaceAncestors"));
        Assert.Equal(resolvedBefore, await RowsWritten("ModuleResolvedSettings"));
    }

    [Fact]
    public async Task Moving_a_namespace_rewrites_only_the_ancestry_of_what_moved()
    {
        var from = await _scenario.AddNamespace("00");
        var to = await _scenario.AddNamespace("00");
        var moved = await _scenario.AddNamespace("00", from.Id);
        await _scenario.AddNamespace("00", moved.Id);
        await _scenario.AddNamespace("00", from.Id);

        var closureBefore = await RowsWritten("NamespaceAncestors");

        var stored = await _scenario.Load(moved.Id);
        stored.ParentNamespaceId = to.Id;
        await _scenario.NamespaceRepositoryFor(_owner).Update(stored);

        // The moved Namespace and the one below it each lose a row for the old parent and gain one for the new.
        Assert.Equal(closureBefore + 4, await RowsWritten("NamespaceAncestors"));
    }

    private Task<long> RowsWritten(string table) => _scenario.RowsWritten(table);

    private async Task<ModuleResolvedSetting> Row(Guid moduleId)
    {
        await using var db = fixture.CreateDbContext();
        return await db.ModuleResolvedSettings.AsNoTracking().SingleAsync(r => r.ModuleId == moduleId);
    }
}
