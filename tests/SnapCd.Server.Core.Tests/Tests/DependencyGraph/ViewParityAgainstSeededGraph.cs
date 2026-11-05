// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Services.Nonsecured.DependencyGraph;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Interfaces;
using SnapCd.Server.Host.Database;
using Xunit;
using Xunit.Abstractions;

namespace SnapCd.Server.Core.Tests.Tests.DependencyGraph;

/// <summary>
/// The same parity comparison as ViewParityTests, but against a database seeded to a realistic
/// size rather than the shared fixture.
///
/// Skipped unless SNAPCD_PARITY_CONNECTION is set, so it does not run in CI or in an ordinary
/// suite run. Point it at a bench database with `_planning/.../scripts/parity-check.sh`, which
/// is built by `bench-up.sh` with 1000 Modules and 1079 edges across both halves of the UNION.
///
/// The fixture-backed test is the regression guard. This one is the evidence.
/// </summary>
public class ViewParityAgainstSeededGraph
{
    private readonly ITestOutputHelper _output;

    private static string? Connection => Environment.GetEnvironmentVariable("SNAPCD_PARITY_CONNECTION");

    public ViewParityAgainstSeededGraph(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task Dependencies_LinqMatchesTheView()
    {
        // Returning rather than skipping: xunit v2 only takes a constant Skip reason. The line
        // below is the only thing distinguishing "did not run" from "passed", so read it.
        if (string.IsNullOrWhiteSpace(Connection))
        {
            _output.WriteLine("SKIPPED: SNAPCD_PARITY_CONNECTION not set. "
                              + "Run _planning/122-.../scripts/parity-check.sh against a bench database.");
            return;
        }

        await using var db = CreateContext();

        var fromView = await db.Dependencies.AsNoTracking().ToListAsync();
        var fromLinq = await DependencyGraphQueries.Dependencies(db).AsNoTracking().ToListAsync();

        _output.WriteLine($"view {fromView.Count} rows, LINQ {fromLinq.Count} rows");
        _output.WriteLine($"  of which standalone: view {fromView.Count(d => d.ReferencedModuleId == null)}, "
                          + $"LINQ {fromLinq.Count(d => d.ReferencedModuleId == null)}");

        Assert.True(fromView.Count > 100,
            $"only {fromView.Count} rows: this is not the seeded bench database.");
        Assert.Contains(fromView, d => d.ReferencedModuleId == null);
        Assert.Contains(fromView, d => d.ReferencedModuleId != null);

        var viewKeys = fromView.Select(DependencyKey.Of).ToHashSet();
        var linqKeys = fromLinq.Select(DependencyKey.Of).ToHashSet();

        foreach (var row in viewKeys.Except(linqKeys).Take(5)) _output.WriteLine($"  view only: {row}");
        foreach (var row in linqKeys.Except(viewKeys).Take(5)) _output.WriteLine($"  LINQ only: {row}");

        Assert.Empty(viewKeys.Except(linqKeys));
        Assert.Empty(linqKeys.Except(viewKeys));
        Assert.Equal(fromView.Count, fromLinq.Count);
    }

    [Fact]
    public async Task RecursiveApply_LinqMatchesTheView()
    {
        if (string.IsNullOrWhiteSpace(Connection)) { _output.WriteLine("SKIPPED: SNAPCD_PARITY_CONNECTION not set."); return; }

        await using var db = CreateContext();

        var fromView = await db.RecursiveApplyDependencies.AsNoTracking().ToListAsync();
        var fromLinq = await DependencyGraphQueries.RecursiveApply(db).AsNoTracking().ToListAsync();

        CompareRecursive(fromView, fromLinq, "vw_RecursiveApplyDependencies");
    }

    [Fact]
    public async Task RecursiveDestroy_LinqMatchesTheView()
    {
        if (string.IsNullOrWhiteSpace(Connection)) { _output.WriteLine("SKIPPED: SNAPCD_PARITY_CONNECTION not set."); return; }

        await using var db = CreateContext();

        var fromView = await db.RecursiveDestroyDependencies.AsNoTracking().ToListAsync();
        var fromLinq = await DependencyGraphQueries.RecursiveDestroy(db).AsNoTracking().ToListAsync();

        CompareRecursive(fromView, fromLinq, "vw_RecursiveDestroyDependencies");
    }

    /// <summary>
    /// The recursive rows compared whole, Root side and Depth included, rather than converted to
    /// a Dependency first: that conversion drops both.
    /// </summary>
    private void CompareRecursive<T>(IReadOnlyList<T> view, IReadOnlyList<T> linq, string what)
        where T : IRecursiveDependency
    {
        _output.WriteLine($"{what}: view {view.Count}, LINQ {linq.Count}");
        _output.WriteLine($"  standalone: view {view.Count(d => d.ReferencedModuleId == null)}, "
                          + $"LINQ {linq.Count(d => d.ReferencedModuleId == null)}");

        Assert.True(view.Count > 100, $"only {view.Count} rows: this is not the seeded bench database.");
        Assert.Contains(view, d => d.ReferencedModuleId == null);
        Assert.Contains(view, d => d.ReferencedModuleId != null);
        Assert.Contains(view, d => d.Depth > 0);

        var viewKeys = view.Select(d => DependencyKey.OfRecursive(d)).ToHashSet();
        var linqKeys = linq.Select(d => DependencyKey.OfRecursive(d)).ToHashSet();

        foreach (var row in viewKeys.Except(linqKeys).Take(3)) _output.WriteLine($"  view only: {row}");
        foreach (var row in linqKeys.Except(viewKeys).Take(3)) _output.WriteLine($"  LINQ only: {row}");

        Assert.Empty(viewKeys.Except(linqKeys));
        Assert.Empty(linqKeys.Except(viewKeys));
        Assert.Equal(view.Count, linq.Count);
    }

    /// <summary>
    /// Both sides as keys over every scalar column, diffed as sets. The guards reject a pass over
    /// data that would not have exercised both halves of the UNION.
    /// </summary>
    private void Compare(IReadOnlyList<Dependency> view, IReadOnlyList<Dependency> linq, string what)
    {
        _output.WriteLine($"{what}: view {view.Count}, LINQ {linq.Count}");
        _output.WriteLine($"  standalone: view {view.Count(d => d.ReferencedModuleId == null)}, "
                          + $"LINQ {linq.Count(d => d.ReferencedModuleId == null)}");

        Assert.True(view.Count > 100, $"only {view.Count} rows: this is not the seeded bench database.");
        Assert.Contains(view, d => d.ReferencedModuleId == null);
        Assert.Contains(view, d => d.ReferencedModuleId != null);

        var viewKeys = view.Select(DependencyKey.Of).ToHashSet();
        var linqKeys = linq.Select(DependencyKey.Of).ToHashSet();

        foreach (var row in viewKeys.Except(linqKeys).Take(3)) _output.WriteLine($"  view only: {row}");
        foreach (var row in linqKeys.Except(viewKeys).Take(3)) _output.WriteLine($"  LINQ only: {row}");

        Assert.Empty(viewKeys.Except(linqKeys));
        Assert.Empty(linqKeys.Except(viewKeys));
        Assert.Equal(view.Count, linq.Count);
    }

    private static SnapCdDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SelfHostedSnapCdDbContext>()
            .UseSqlServer(Connection, m => m.MigrationsAssembly("SnapCd.Server.Host"))
            .ConfigureWarnings(w =>
                w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;

        return new SelfHostedSnapCdDbContext(options);
    }
}
