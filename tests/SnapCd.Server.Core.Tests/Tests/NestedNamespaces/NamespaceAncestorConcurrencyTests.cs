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

        await using var check = fixture.CreateDbContext();
        var createdIds = created.Select(n => n.Id).ToList();
        var withOwnRow = await check.NamespaceAncestors
            .CountAsync(a => createdIds.Contains(a.NamespaceId) && a.AncestorNamespaceId == a.NamespaceId && a.Depth == 0);
        var withParentRow = await check.NamespaceAncestors
            .CountAsync(a => createdIds.Contains(a.NamespaceId) && a.AncestorNamespaceId == parent.Id && a.Depth == 1);

        Assert.True(failures.Count == 0 && withOwnRow == created.Count && withParentRow == created.Count,
            $"{failures.Count} of {Workers * Rounds} concurrent creates failed. Of the {created.Count} that succeeded, " +
            $"{created.Count - withOwnRow} have no ancestry row for themselves and {created.Count - withParentRow} have none for their parent." +
            Environment.NewLine + string.Join(Environment.NewLine, failures.Take(5)));

        Assert.Equal(0, await _scenario.RowsTheRepairWrites());
    }

    [Fact]
    public async Task Namespaces_created_under_one_that_is_being_moved_end_up_with_the_chain_it_has()
    {
        var left = await _scenario.AddNamespace("00");
        var right = await _scenario.AddNamespace("00");
        var moving = await _scenario.AddNamespace("00", left.Id);
        var failures = new List<string>();
        var created = new List<Namespace>();
        var sync = new object();

        for (var round = 1; round <= Rounds; round++)
        {
            using var gate = new SemaphoreSlim(0, Workers);
            var thisRound = round;
            var destination = round % 2 == 0 ? left.Id : right.Id;

            var writers = Enumerable.Range(0, Workers).Select(async worker =>
            {
                await using var db = fixture.CreateDbContext();
                await db.Database.OpenConnectionAsync();
                await gate.WaitAsync();

                try
                {
                    if (worker == 0)
                    {
                        await db.Namespaces.Where(n => n.Id == moving.Id)
                            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ParentNamespaceId, destination));
                        return;
                    }

                    var ns = new Namespace
                    {
                        Id = Guid.NewGuid(),
                        Name = $"nested_{Guid.NewGuid():N}",
                        StackId = fixture.Stacks["00"].Id,
                        OrganizationId = _scenario.OrganizationId,
                        ParentNamespaceId = moving.Id,
                        CreatedDateTime = DateTime.UtcNow
                    };

                    db.Namespaces.Add(ns);
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
            $"{failures.Count} of {Workers * Rounds} writes failed:{Environment.NewLine}" +
            string.Join(Environment.NewLine, failures.Take(5)));

        var endedUnder = Rounds % 2 == 0 ? left.Id : right.Id;
        foreach (var ns in created)
            Assert.Equal([moving.Id, endedUnder], await _scenario.AncestorsOf(ns.Id));

        Assert.Equal(0, await _scenario.RowsTheRepairWrites());
    }

    [Fact]
    public async Task Two_moves_that_would_together_form_a_cycle_do_not_both_succeed()
    {
        for (var round = 1; round <= Rounds; round++)
        {
            var first = await _scenario.AddNamespace("00");
            var second = await _scenario.AddNamespace("00");
            using var gate = new SemaphoreSlim(0, 2);

            // Written past the repository, whose own check each of these would pass on its own.
            var moves = new[] { (first.Id, second.Id), (second.Id, first.Id) }.Select(async move =>
            {
                await using var db = fixture.CreateDbContext();
                await db.Database.OpenConnectionAsync();
                await gate.WaitAsync();

                try
                {
                    await db.Namespaces.Where(n => n.Id == move.Item1)
                        .ExecuteUpdateAsync(s => s.SetProperty(n => n.ParentNamespaceId, move.Item2));
                    return true;
                }
                catch (Exception e) when (Refused(e))
                {
                    return false;
                }
            }).ToArray();

            gate.Release(2);
            var succeeded = (await Task.WhenAll(moves)).Count(moved => moved);

            Assert.Equal(1, succeeded);

            var above = (await _scenario.AncestorsOf(first.Id)).Concat(await _scenario.AncestorsOf(second.Id));
            Assert.Single(above);
        }

        Assert.Equal(0, await _scenario.RowsTheRepairWrites());
    }

    /// <summary>
    /// The two ways the second move is turned away: the trigger sees the cycle, or the server picks
    /// it as the victim of the deadlock the two foreign key checks form on each other's row.
    /// </summary>
    private static bool Refused(Exception e)
    {
        const int Deadlock = 1205;
        const int WouldFormCycle = 51001;

        for (Exception? cause = e; cause != null; cause = cause.InnerException)
            if (cause is Microsoft.Data.SqlClient.SqlException { Number: Deadlock or WouldFormCycle })
                return true;

        return false;
    }
}
