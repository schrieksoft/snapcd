// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.Utils;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// An inherited default is resolved in more than one place: by a fold over the loaded chain when a
/// job resolves its Module or a source change is matched to it, and by the NamespaceResolvedDefaults
/// view for every query. Different callers read each, so they have to give the same answer.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ResolvedDefaultsAgreementTests(Fixture fixture) : IAsyncLifetime
{
    // Every combination of unset, loose and strict with no pin, Forbidden and OnlyStricter.
    private const int Variants = 9;
    private const int Depth = 3;

    private NestedScenario _scenario = null!;
    private readonly List<List<Namespace>> _levels = [];

    public async Task InitializeAsync()
    {
        _scenario = new NestedScenario(fixture);

        List<Namespace?> parents = [null];

        for (var level = 0; level < Depth; level++)
        {
            var created = parents
                .SelectMany(parent => Enumerable.Range(0, Variants).Select(variant => Build(parent, variant)))
                .ToList();

            await using var db = fixture.CreateDbContext();
            db.Namespaces.AddRange(created);
            await db.SaveChangesAsync();

            _levels.Add(created);
            parents = created.Cast<Namespace?>().ToList();
        }
    }

    public async Task DisposeAsync()
    {
        await using var db = fixture.CreateDbContext();

        // A parent is restricted from its children, so the deepest level goes first.
        foreach (var level in _levels.AsEnumerable().Reverse())
        {
            var ids = level.Select(n => n.Id).ToList();
            await db.Namespaces.Where(n => ids.Contains(n.Id)).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task The_fold_and_the_view_resolve_every_chain_to_the_same_values()
    {
        var namespaces = _levels.SelectMany(l => l).ToList();
        var ids = namespaces.Select(n => n.Id).ToList();

        await using var db = fixture.CreateDbContext();
        var view = await db.NamespaceResolvedDefaults
            .Where(d => ids.Contains(d.NamespaceId))
            .ToDictionaryAsync(d => d.NamespaceId);

        Assert.Equal(namespaces.Count, view.Count);

        var byId = namespaces.ToDictionary(n => n.Id);
        using var resolver = _scenario.Resolver();
        var mismatches = new List<string>();

        foreach (var ns in namespaces)
        {
            var module = ModuleIn(ns);
            var folded = await resolver.GetDeclared(module);
            var row = view[ns.Id];

            Compare("TriggerPathFilterEnabled",
                TriggerPathClosure.FilterEnabled(module, ChainOf(ns, byId)), row.TriggerPathFilterEnabled ?? false);

            Compare("CleanInitEnabled", folded.CleanInitEnabled, row.CleanInitEnabled ?? false);
            Compare("DriftCheckEnabled", folded.DriftCheckEnabled, row.DriftCheckEnabled ?? false);
            Compare("DriftCheckIntervalMinutes", folded.DriftCheckIntervalMinutes, row.DriftCheckIntervalMinutes);
            Compare("ApprovalTimeoutMinutes", folded.ApprovalTimeoutMinutes, row.ApprovalTimeoutMinutes);
            Compare("Engine", folded.Engine, EngineName(row.Engine));

            void Compare<T>(string setting, T fromFold, T fromView)
            {
                if (!EqualityComparer<T>.Default.Equals(fromFold, fromView))
                    mismatches.Add($"{ns.Name} {setting}: fold {fromFold?.ToString() ?? "null"}, view {fromView?.ToString() ?? "null"}");
            }
        }

        Assert.True(mismatches.Count == 0,
            $"{mismatches.Count} mismatches over {namespaces.Count} chains:{Environment.NewLine}" +
            string.Join(Environment.NewLine, mismatches.Take(15)));
    }

    /// <summary>
    /// One Namespace of the tree. Each setting takes its value and pin from a different rearrangement
    /// of the variant, so they are not copies of one case.
    /// </summary>
    private Namespace Build(Namespace? parent, int variant)
    {
        var (cleanValue, cleanPin) = Split(variant);
        var (driftValue, driftPin) = Split(variant * 2 % Variants);
        var (intervalValue, intervalPin) = Split(variant * 4 % Variants);
        var (timeoutValue, timeoutPin) = Split(variant * 5 % Variants);
        var (engineValue, enginePin) = Split(variant * 7 % Variants);
        var (filterValue, filterPin) = Split(variant * 8 % Variants);

        return new Namespace
        {
            Id = Guid.NewGuid(),
            Name = $"{parent?.Name ?? $"agree_{Guid.NewGuid():N}"}_{variant}",
            StackId = fixture.Stacks["00"].Id,
            OrganizationId = _scenario.OrganizationId,
            ParentNamespaceId = parent?.Id,
            CreatedDateTime = DateTime.UtcNow,

            DefaultCleanInitEnabled = cleanValue == 0 ? null : cleanValue == 2,
            OverrideOnCleanInitEnabled = Ordered(cleanPin),
            DefaultTriggerPathFilterEnabled = filterValue == 0 ? null : filterValue == 2,
            OverrideOnTriggerPathFilterEnabled = Ordered(filterPin),
            DefaultDriftCheckEnabled = driftValue == 0 ? null : driftValue == 2,
            OverrideOnDriftCheckEnabled = Ordered(driftPin),
            DefaultDriftCheckIntervalMinutes = intervalValue == 0 ? null : intervalValue * 10,
            OverrideOnDriftCheckIntervalMinutes = Unordered(intervalPin),
            DefaultApprovalTimeoutMinutes = timeoutValue == 0 ? null : timeoutValue * 10,
            OverrideOnApprovalTimeoutMinutes = Unordered(timeoutPin),
            DefaultEngine = engineValue == 0
                ? null
                : engineValue == 1 ? StateManagementEngine.Terraform : StateManagementEngine.Pulumi,
            OverrideOnEngine = Unordered(enginePin)
        };
    }

    /// <summary>The Namespace and those above it, nearest first.</summary>
    private static List<Namespace> ChainOf(Namespace ns, Dictionary<Guid, Namespace> byId)
    {
        var chain = new List<Namespace> { ns };

        while (chain[^1].ParentNamespaceId is { } parentId) chain.Add(byId[parentId]);

        return chain;
    }

    private static (int Value, int Pin) Split(int variant) => (variant / 3, variant % 3);

    private static OrderedOverridePolicy? Ordered(int pin) => pin switch
    {
        1 => OrderedOverridePolicy.Forbidden,
        2 => OrderedOverridePolicy.OnlyStricter,
        _ => null
    };

    private static OverridePolicy? Unordered(int pin) => pin == 1 ? OverridePolicy.Forbidden : null;

    private static string EngineName(StateManagementEngine? engine) => engine switch
    {
        StateManagementEngine.Terraform => "terraform",
        StateManagementEngine.Pulumi => "pulumi",
        _ => "tofu"
    };

    /// <summary>A Module that sets nothing itself, so what it resolves is what its chain supplies.</summary>
    private Module ModuleIn(Namespace ns) => new()
    {
        Id = Guid.NewGuid(),
        Name = "agreement",
        OrganizationId = ns.OrganizationId,
        NamespaceId = ns.Id,
        Namespace = new Namespace
        {
            Id = ns.Id, Name = ns.Name, OrganizationId = ns.OrganizationId,
            StackId = ns.StackId, Stack = fixture.Stacks["00"]
        },
        RunnerId = fixture.Runners["0"].Id,
        Runner = fixture.Runners["0"],
        SourceUrl = "https://github.com/test/agreement",
        SourceRevision = "main"
    };
}
