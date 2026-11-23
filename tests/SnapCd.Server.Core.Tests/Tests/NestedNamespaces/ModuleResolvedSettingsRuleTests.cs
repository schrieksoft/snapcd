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
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Tests.Tests.NestedNamespaces;

/// <summary>
/// ModuleResolvedSettings is computed in SQL and nothing else in the product computes it, so this
/// is the check that the procedure implements the rule. Each stored row is compared against the
/// rule written the other way round: a walk up the chain, where the procedure aggregates over it.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class ModuleResolvedSettingsRuleTests(Fixture fixture) : IAsyncLifetime
{
    // Every combination of unset, loose and strict with no pin, Forbidden and OnlyStricter.
    private const int Variants = 9;
    private const int Depth = 3;

    private enum Pin { None, Forbidden, OnlyStricter }

    /// <summary>One inheritable setting: how to write it on a Namespace and a Module, and read it back resolved.</summary>
    private sealed record Setting(
        string Name,
        bool Ordered,
        Action<Namespace, int?, Pin> SetOnNamespace,
        Action<Module, int?> SetOnModule,
        Func<ModuleResolvedSetting, int?> Resolved);

    private static bool? Flag(int? v) => v == null ? null : v == 2;
    private static int? FlagCode(bool? v) => v == null ? null : v.Value ? 2 : 1;
    private static OrderedOverridePolicy? Ordered(Pin pin) => pin switch
    {
        Pin.Forbidden => OrderedOverridePolicy.Forbidden,
        Pin.OnlyStricter => OrderedOverridePolicy.OnlyStricter,
        _ => null
    };
    private static OverridePolicy? Unordered(Pin pin) => pin == Pin.Forbidden ? OverridePolicy.Forbidden : null;
    private static StateManagementEngine? Engine(int? v) => v switch { 1 => StateManagementEngine.Terraform, 2 => StateManagementEngine.Pulumi, _ => null };
    private static OutputSetDownstreamRestriction? Restriction(int? v) => v switch { 1 => OutputSetDownstreamRestriction.SensitiveOnly, 2 => OutputSetDownstreamRestriction.All, _ => null };
    private static OutputRestrictionExemption? Exemption(int? v) => v switch { 1 => OutputRestrictionExemption.OwnNamespaceAndDescendants, 2 => OutputRestrictionExemption.OwnNamespace, _ => null };

    private const int ExemptionIndex = 10;

    private static readonly Setting[] Settings =
    [
        new("CleanInitEnabled", true,
            (n, v, p) => { n.DefaultCleanInitEnabled = Flag(v); n.OverrideOnCleanInitEnabled = Ordered(p); },
            (m, v) => m.CleanInitEnabled = Flag(v), r => FlagCode(r.CleanInitEnabled)),
        new("TriggerPathFilterEnabled", true,
            (n, v, p) => { n.DefaultTriggerPathFilterEnabled = Flag(v); n.OverrideOnTriggerPathFilterEnabled = Ordered(p); },
            (m, v) => m.TriggerPathFilterEnabled = Flag(v), r => FlagCode(r.TriggerPathFilterEnabled)),
        new("ApplyApprovalThreshold", true,
            (n, v, p) => { n.DefaultApplyApprovalThreshold = v; n.OverrideOnApplyApprovalThreshold = Ordered(p); },
            (m, v) => m.ApplyApprovalThreshold = v, r => r.ApplyApprovalThreshold),
        new("DestroyApprovalThreshold", true,
            (n, v, p) => { n.DefaultDestroyApprovalThreshold = v; n.OverrideOnDestroyApprovalThreshold = Ordered(p); },
            (m, v) => m.DestroyApprovalThreshold = v, r => r.DestroyApprovalThreshold),
        new("StateMigrationApprovalThreshold", true,
            (n, v, p) => { n.DefaultStateMigrationApprovalThreshold = v; n.OverrideOnStateMigrationApprovalThreshold = Ordered(p); },
            (m, v) => m.StateMigrationApprovalThreshold = v, r => r.StateMigrationApprovalThreshold),
        new("ApprovalTimeoutMinutes", false,
            (n, v, p) => { n.DefaultApprovalTimeoutMinutes = v; n.OverrideOnApprovalTimeoutMinutes = Unordered(p); },
            (m, v) => m.ApprovalTimeoutMinutes = v, r => r.ApprovalTimeoutMinutes),
        new("Engine", false,
            (n, v, p) => { n.DefaultEngine = Engine(v); n.OverrideOnEngine = Unordered(p); },
            (m, v) => m.Engine = Engine(v),
            r => r.Engine switch { StateManagementEngine.Terraform => 1, StateManagementEngine.Pulumi => 2, _ => null }),
        new("DriftCheckEnabled", true,
            (n, v, p) => { n.DefaultDriftCheckEnabled = Flag(v); n.OverrideOnDriftCheckEnabled = Ordered(p); },
            (m, v) => m.DriftCheckEnabled = Flag(v), r => FlagCode(r.DriftCheckEnabled)),
        new("DriftCheckIntervalMinutes", false,
            (n, v, p) => { n.DefaultDriftCheckIntervalMinutes = v; n.OverrideOnDriftCheckIntervalMinutes = Unordered(p); },
            (m, v) => m.DriftCheckIntervalMinutes = v, r => r.DriftCheckIntervalMinutes),
        new("OutputSetDownstreamRestriction", true,
            (n, v, p) => { n.DefaultOutputSetDownstreamRestriction = Restriction(v); n.OverrideOnOutputSetDownstreamRestriction = Ordered(p); },
            (m, v) => m.OutputSetDownstreamRestriction = Restriction(v),
            r => r.OutputSetDownstreamRestriction switch { OutputSetDownstreamRestriction.SensitiveOnly => 1, OutputSetDownstreamRestriction.All => 2, _ => null }),
        new("OutputRestrictionExemption", true,
            (n, v, p) => { n.DefaultOutputRestrictionExemption = Exemption(v); n.OverrideOnOutputRestrictionExemption = Ordered(p); },
            (m, v) => m.OutputRestrictionExemption = Exemption(v),
            r => r.OutputRestrictionExemption switch { OutputRestrictionExemption.OwnNamespaceAndDescendants => 1, OutputRestrictionExemption.OwnNamespace => 2, _ => null })
    ];

    private sealed record Link(Guid NamespaceId, int?[] Values, Pin[] Pins);

    private readonly List<List<Namespace>> _levels = [];
    private readonly Dictionary<Guid, Link> _namespaceLinks = [];
    private readonly Dictionary<Guid, Guid?> _parents = [];
    private readonly Dictionary<Guid, Link> _moduleLinks = [];
    private Guid _organizationId;

    public async Task InitializeAsync()
    {
        _organizationId = fixture.Organizations["0"].Id;

        List<Namespace?> parents = [null];

        for (var level = 0; level < Depth; level++)
        {
            var created = parents
                .SelectMany(parent => Enumerable.Range(0, Variants).Select(variant => BuildNamespace(parent, variant)))
                .ToList();

            await using var db = fixture.CreateDbContext();
            db.Namespaces.AddRange(created);
            await db.SaveChangesAsync();

            _levels.Add(created);
            parents = created.Cast<Namespace?>().ToList();
        }

        // One Module in every Namespace, so each is the near end of one distinct chain.
        await using var moduleDb = fixture.CreateDbContext();
        var index = 0;

        foreach (var ns in _levels.SelectMany(l => l))
        {
            var module = fixture.CreateTestModule($"rule_{Guid.NewGuid():N}", "000", moduleDb);
            module.NamespaceId = ns.Id;

            var values = new int?[Settings.Length];
            for (var s = 0; s < Settings.Length; s++)
            {
                var value = (index * (s + 2) + s) % 3;
                values[s] = value == 0 ? null : value;
                Settings[s].SetOnModule(module, values[s]);
            }

            _moduleLinks[module.Id] = new Link(ns.Id, values, Enumerable.Repeat(Pin.None, Settings.Length).ToArray());
            index++;
        }

        await moduleDb.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = fixture.CreateDbContext();
        var moduleIds = _moduleLinks.Keys.ToList();

        await db.ClearRowsBlockingModuleDelete(m => moduleIds.Contains(m.Id), _organizationId);
        await db.Modules.Where(m => moduleIds.Contains(m.Id)).ExecuteDeleteAsync();

        // A parent is restricted from its children, so the deepest level goes first.
        foreach (var level in _levels.AsEnumerable().Reverse())
        {
            var ids = level.Select(n => n.Id).ToList();
            await db.Namespaces.Where(n => ids.Contains(n.Id)).ExecuteDeleteAsync();
        }
    }

    [Fact]
    public async Task Every_stored_row_is_what_a_walk_up_the_chain_gives()
    {
        var rows = await StoredRows();
        Assert.Equal(_moduleLinks.Count, rows.Count);

        var mismatches = new List<string>();

        foreach (var (moduleId, moduleLink) in _moduleLinks)
        {
            var chain = ChainOf(moduleLink);
            var row = rows[moduleId];

            for (var s = 0; s < Settings.Length; s++)
            {
                var (expected, declaredBy) = Walk(chain, s, Settings[s].Ordered);
                var stored = Settings[s].Resolved(row);

                if (expected != stored)
                    mismatches.Add($"{Describe(chain, s)} {Settings[s].Name}: walk {Show(expected)}, stored {Show(stored)}");

                if (s == ExemptionIndex && declaredBy != row.OutputRestrictionExemptionNamespaceId)
                    mismatches.Add($"{Describe(chain, s)} exemption set on: walk {declaredBy}, stored {row.OutputRestrictionExemptionNamespaceId}");
            }
        }

        Assert.True(mismatches.Count == 0,
            $"{mismatches.Count} mismatches over {_moduleLinks.Count} chains:{Environment.NewLine}" +
            string.Join(Environment.NewLine, mismatches.Take(15)));
    }

    [Fact]
    public async Task What_the_triggers_wrote_is_what_a_full_recompute_writes()
    {
        var maintained = await StoredRows();

        await using (var db = fixture.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync(
                "DECLARE @every dbo.GuidList; EXEC dbo.usp_RecomputeModuleResolvedSettings @ModuleIds = @every;");

        var recomputed = await StoredRows();

        foreach (var (moduleId, before) in maintained)
            Assert.Equivalent(before, recomputed[moduleId], strict: true);
    }

    private async Task<Dictionary<Guid, ModuleResolvedSetting>> StoredRows()
    {
        var moduleIds = _moduleLinks.Keys.ToList();

        await using var db = fixture.CreateDbContext();
        return await db.ModuleResolvedSettings
            .AsNoTracking()
            .Where(r => moduleIds.Contains(r.ModuleId))
            .ToDictionaryAsync(r => r.ModuleId);
    }

    /// <summary>
    /// The rule as a walk, nearest first: the first value met stands until a Namespace further up
    /// forbids an override, or allows only a stricter one and holds a stricter value itself.
    /// </summary>
    private static (int? Value, Guid? DeclaredBy) Walk(List<Link> chain, int setting, bool ordered)
    {
        int? current = null;
        Guid? declaredBy = null;

        foreach (var link in chain)
        {
            if (link.Values[setting] is not { } own) continue;

            var takes = current == null
                        || link.Pins[setting] == Pin.Forbidden
                        || (ordered && link.Pins[setting] == Pin.OnlyStricter && current < own);

            if (!takes) continue;

            current = own;
            declaredBy = link.NamespaceId;
        }

        return (current, declaredBy);
    }

    /// <summary>The Module, then its Namespace, then each Namespace above that.</summary>
    private List<Link> ChainOf(Link module)
    {
        var chain = new List<Link> { module };

        for (Guid? id = module.NamespaceId; id != null; id = _parents[id.Value])
            chain.Add(_namespaceLinks[id.Value]);

        return chain;
    }

    private static string Describe(List<Link> chain, int setting) =>
        string.Join(" < ", chain.Select(l => $"{Show(l.Values[setting])}{(l.Pins[setting] == Pin.None ? "" : "/" + l.Pins[setting])}"));

    private static string Show(int? value) => value?.ToString() ?? "unset";

    /// <summary>
    /// One Namespace of the tree. Each setting takes its value and pin from a different rearrangement
    /// of the variant, so the settings are not copies of one case.
    /// </summary>
    private Namespace BuildNamespace(Namespace? parent, int variant)
    {
        var ns = new Namespace
        {
            Id = Guid.NewGuid(),
            Name = $"{parent?.Name ?? $"rule_{Guid.NewGuid():N}"}_{variant}",
            StackId = fixture.Stacks["00"].Id,
            OrganizationId = _organizationId,
            ParentNamespaceId = parent?.Id,
            CreatedDateTime = DateTime.UtcNow
        };

        int[] multipliers = [1, 2, 4, 5, 7, 8];
        var values = new int?[Settings.Length];
        var pins = new Pin[Settings.Length];

        for (var s = 0; s < Settings.Length; s++)
        {
            var rearranged = (variant * multipliers[s % multipliers.Length] + s) % Variants;
            values[s] = rearranged / 3 == 0 ? null : rearranged / 3;
            pins[s] = (Pin)(rearranged % 3);
            if (!Settings[s].Ordered && pins[s] == Pin.OnlyStricter) pins[s] = Pin.None;

            Settings[s].SetOnNamespace(ns, values[s], pins[s]);
        }

        _namespaceLinks[ns.Id] = new Link(ns.Id, values, pins);
        _parents[ns.Id] = parent?.Id;

        return ns;
    }
}
