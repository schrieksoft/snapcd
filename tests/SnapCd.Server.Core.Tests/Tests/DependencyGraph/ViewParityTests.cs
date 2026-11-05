// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Services.Nonsecured.DependencyGraph;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace SnapCd.Server.Core.Tests.Tests.DependencyGraph;

/// <summary>
/// Holds a LINQ replacement for a dependency-graph view to the view it replaces: same rows,
/// same values, every column.
///
/// A view and an EF query can disagree in ways neither the compiler nor a row count will show -
/// a column dropped from a projection, a LEFT where the view has an INNER, a null coalesced on
/// one side only. The views are deleted once their replacement passes here, so this is the step
/// that has to be trusted.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ViewParityTests
{
    private readonly Fixture _fixture;
    private readonly ITestOutputHelper _output;

    public ViewParityTests(Fixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    /// <summary>
    /// The shared fixture's data is whatever the other 82 test classes happen to have created,
    /// so this asserts the comparison had something to compare before trusting an empty diff.
    /// An equality test over two empty sets passes and proves nothing.
    /// </summary>
    [Fact]
    public async Task VwDependencies_MatchesLinqEquivalent()
    {
        await using var db = _fixture.CreateDbContext();

        var fromView = await db.Dependencies.AsNoTracking().ToListAsync();
        var fromLinq = await DependencyGraphQueries.Dependencies(db).AsNoTracking().ToListAsync();

        AssertParity(fromView, fromLinq, nameof(db.Dependencies));
    }

    /// <summary>
    /// Both sides as sorted keys, so a difference names the row and the column rather than
    /// reporting that two lists are not equal.
    /// </summary>
    private void AssertParity(IReadOnlyList<Dependency> view, IReadOnlyList<Dependency> linq, string what)
    {
        _output.WriteLine($"{what}: {view.Count} rows from the view, {linq.Count} from LINQ");

        Assert.True(view.Count > 0,
            $"{what} returned no rows, so this comparison proves nothing. Seed the fixture "
            + "with Modules that have dependencies before trusting a pass.");

        Assert.True(view.Any(d => d.ReferencedModuleId == null),
            $"{what} returned no standalone rows, so the second half of the UNION is untested. "
            + "Seed at least one Module with no edges on either side.");

        Assert.True(view.Any(d => d.ReferencedModuleId != null),
            $"{what} returned no edge rows, so the first half of the UNION is untested.");

        var onlyInView = view.Select(DependencyKey.Of).Except(linq.Select(DependencyKey.Of)).Take(5).ToList();
        var onlyInLinq = linq.Select(DependencyKey.Of).Except(view.Select(DependencyKey.Of)).Take(5).ToList();

        foreach (var row in onlyInView) _output.WriteLine($"  view only: {row}");
        foreach (var row in onlyInLinq) _output.WriteLine($"  LINQ only: {row}");

        Assert.Empty(onlyInView);
        Assert.Empty(onlyInLinq);
        Assert.Equal(view.Count, linq.Count);
    }
}
