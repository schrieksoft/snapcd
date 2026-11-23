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

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// Namespaces created in one organization at the same moment each maintain the same ancestry rows.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class NamespaceAncestorConcurrencyTests(Fixture fixture) : IAsyncLifetime
{
    private const int Workers = 8;
    private const int Rounds = 15;

    private NestedScenario _scenario = null!;

    public Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _scenario.DisposeAsync();

    [Fact]
    public async Task Namespaces_created_at_the_same_moment_all_succeed_and_all_get_their_ancestry()
    {
        var parent = await _scenario.AddNamespace("00");
        var failures = new List<string>();
        var created = new List<Namespace>();
        var sync = new object();

        for (var round = 1; round <= Rounds; round++)
        {
            using var gate = new SemaphoreSlim(0, Workers);
            var thisRound = round;

            var writers = Enumerable.Range(0, Workers).Select(async worker =>
            {
                var ns = new Namespace
                {
                    Id = Guid.NewGuid(),
                    Name = $"nested_{Guid.NewGuid():N}",
                    StackId = fixture.Stacks["00"].Id,
                    OrganizationId = _scenario.OrganizationId,
                    ParentNamespaceId = parent.Id,
                    CreatedDateTime = DateTime.UtcNow
                };

                await using var db = fixture.CreateDbContext();
                await db.Database.OpenConnectionAsync();
                db.Namespaces.Add(ns);
                await gate.WaitAsync();

                try
                {
                    await db.SaveChangesAsync();
                    lock (sync) created.Add(ns);
                }
                catch (Exception e)
                {
                    var cause = e.InnerException ?? e;
                    lock (sync) failures.Add($"round {thisRound} worker {worker}: {cause.GetType().Name} {cause.Message}");
                }
            }).ToArray();

            gate.Release(Workers);
            await Task.WhenAll(writers);
        }

        foreach (var ns in created) _scenario.Track(ns);

        Assert.True(failures.Count == 0,
            $"{failures.Count} of {Workers * Rounds} concurrent creates failed:{Environment.NewLine}" +
            string.Join(Environment.NewLine, failures.Take(10)));

        await using var check = fixture.CreateDbContext();
        var createdIds = created.Select(n => n.Id).ToList();
        var withParentRow = await check.NamespaceAncestors
            .CountAsync(a => createdIds.Contains(a.NamespaceId) && a.AncestorNamespaceId == parent.Id && a.Depth == 1);

        Assert.Equal(created.Count, withParentRow);
    }
}
