// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.StateMachine.SplitMonolith.Activites;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.ManualJobs;

/// <summary>
/// The approval threshold for a state migration resolves Module, then Namespace default, then 1.
/// Unlike apply and destroy the fallback is one, because the push is irreversible.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ManualJobApprovalThresholdTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _moduleId;
    private Guid _namespaceId;
    private Guid _organizationId;
    private int? _originalModule;
    private int? _originalNamespace;

    public ManualJobApprovalThresholdTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        await using var db = _fixture.CreateDbContext();
        var module = await db.Modules.SingleAsync(m => m.Id == _moduleId);
        _namespaceId = module.NamespaceId;
        _originalModule = module.SplitMonolithApprovalThreshold;
        _originalNamespace = (await db.Namespaces.SingleAsync(n => n.Id == _namespaceId)).DefaultSplitMonolithApprovalThreshold;
    }

    public Task DisposeAsync() => Set(_originalModule, _originalNamespace);

    [Fact]
    public async Task Module_Override_Wins()
    {
        await Set(module: 3, ns: 2);
        Assert.Equal(3, await Resolve());
    }

    [Fact]
    public async Task Namespace_Default_Applies_When_The_Module_Is_Unset()
    {
        await Set(module: null, ns: 2);
        Assert.Equal(2, await Resolve());
    }

    [Fact]
    public async Task Falls_Back_To_One()
    {
        await Set(module: null, ns: null);
        Assert.Equal(1, await Resolve());
    }

    [Fact]
    public async Task Zero_On_The_Module_Is_Respected()
    {
        await Set(module: 0, ns: 2);
        Assert.Equal(0, await Resolve());
    }

    private async Task<int> Resolve()
    {
        await using var db = _fixture.CreateDbContext();
        return await new Exposed(db).Resolve(_moduleId, _organizationId, db);
    }

    private async Task Set(int? module, int? ns)
    {
        await using var db = _fixture.CreateDbContext();
        (await db.Modules.SingleAsync(m => m.Id == _moduleId)).SplitMonolithApprovalThreshold = module;
        (await db.Namespaces.SingleAsync(n => n.Id == _namespaceId)).DefaultSplitMonolithApprovalThreshold = ns;
        await db.SaveChangesAsync();
    }

    private sealed class Exposed(SnapCdDbContext db) : SplitMonolithNeedsApprovalActivity<object>(db)
    {
        public Task<int> Resolve(Guid moduleId, Guid organizationId, SnapCdDbContext dbContext) => ResolveThreshold(moduleId, organizationId, dbContext);
    }
}
