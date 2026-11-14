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
using SnapCd.Server.Core.Entities.Definition.IntegrationEvents;
using SnapCd.Server.Core.Services.Nonsecured.Integrations;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Integrations;

/// <summary>
/// Which event fires when more than one Namespace in a Module's chain declares one for the same
/// integration. The nearest declaration wins, so a child overrides what it inherits.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class IntegrationEventPrecedenceTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private SnapCdDbContext _dbContext = null!;
    private Guid _organizationId;
    private Guid _integrationId;
    private Guid _moduleId;
    private Guid _ownNamespaceId;
    private Guid _parentNamespaceId;
    private readonly List<Guid> _events = [];

    public IntegrationEventPrecedenceTests(Fixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _dbContext = _fixture.CreateDbContext();
        _organizationId = _fixture.Organizations["0"].Id;

        // Supplied to every Module, so only precedence decides the outcome.
        _integrationId = _fixture.Integrations["AllModules"].Id;

        _moduleId = _fixture.Modules["0000"].Id;
        _ownNamespaceId = _fixture.Namespaces["000"].Id;
        _parentNamespaceId = _fixture.Namespaces["001"].Id;

        // A two-level chain: the Module's own Namespace sits inside another.
        await SetParent(_ownNamespaceId, _parentNamespaceId);
    }

    public async Task DisposeAsync()
    {
        if (_events.Count > 0)
            await _dbContext.NamespaceIntegrationEvents
                .Where(e => _events.Contains(e.Id))
                .ExecuteDeleteAsync();

        await SetParent(_ownNamespaceId, null);
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task An_event_on_a_parent_namespace_reaches_a_module_below_it()
    {
        await AddEvent(_parentNamespaceId, "from the parent");

        var matches = await Match();

        var match = Assert.Single(matches);
        Assert.Equal("from the parent", match.Template);
    }

    [Fact]
    public async Task The_nearest_namespace_wins_over_one_further_up()
    {
        await AddEvent(_parentNamespaceId, "from the parent");
        await AddEvent(_ownNamespaceId, "from the module's own namespace");

        var matches = await Match();

        // One event per integration, and it is the nearer declaration.
        var match = Assert.Single(matches);
        Assert.Equal("from the module's own namespace", match.Template);
    }

    [Fact]
    public async Task The_order_the_rows_arrive_in_does_not_decide_it()
    {
        // The nearer one written first, so a query that simply took the first row would be wrong.
        await AddEvent(_ownNamespaceId, "from the module's own namespace");
        await AddEvent(_parentNamespaceId, "from the parent");

        var matches = await Match();

        var match = Assert.Single(matches);
        Assert.Equal("from the module's own namespace", match.Template);
    }

    private async Task<List<IntegrationEventMatcher.IntegrationEventMatch>> Match()
    {
        await using var db = _fixture.CreateDbContext();
        using var matcher = new IntegrationEventMatcher(db, new IntegrationSupplyResolver(db));

        return await matcher.MatchAsync(_moduleId, _organizationId, IntegrationTrigger.JobSucceeded);
    }

    private async Task AddEvent(Guid namespaceId, string template)
    {
        var id = Guid.NewGuid();

        await using var db = _fixture.CreateDbContext();
        db.NamespaceIntegrationEvents.Add(new NamespaceIntegrationEvent
        {
            Id = id,
            OrganizationId = _organizationId,
            NamespaceId = namespaceId,
            IntegrationId = _integrationId,
            Trigger = IntegrationTrigger.JobSucceeded,
            Template = template,
            IsDisabled = false
        });
        await db.SaveChangesAsync();

        _events.Add(id);
    }

    private async Task SetParent(Guid namespaceId, Guid? parentId)
    {
        await using var db = _fixture.CreateDbContext();
        await db.Namespaces
            .Where(n => n.Id == namespaceId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ParentNamespaceId, parentId));
    }
}
