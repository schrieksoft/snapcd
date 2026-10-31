// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Architecture;

/// <summary>
/// A row that restricts on a Stack, Namespace, Module or Module job refuses the delete it should
/// have gone with, so something has to clear it by hand. Nothing in the schema says which rows
/// those are, and a new one is a delete that fails at runtime with no sign of it at build time.
///
/// Read from the model rather than the source: a search for the class maps' wording has already
/// returned a false empty once, because one of them names its navigation differently.
/// </summary>
public class DeleteBlockerCoverageTests
{
    /// <summary>
    /// The tables whose rows <c>DeleteBlockerCleanup</c> clears, by the owner each is reached
    /// from. A new restricting foreign key has to be added here and to that helper together.
    /// </summary>
    private static readonly Dictionary<Type, string[]> Cleared = new()
    {
        // Secrets and the module inputs are each one table holding every subtype.
        [typeof(Module)] =
        [
            "Secrets", "ModuleInputs", "DependsOnModules",
            "Transfers", "StateMigrationJobs", "SignalEdges",
            "ModuleProbeAttempts", "SignalOutputSets", "ResourceChangeSets"
        ],
        [typeof(Namespace)] = ["Secrets", "ModuleInputs"],
        [typeof(Stack)] = ["Secrets"],
        [typeof(ModuleJob)] =
        [
            "JobRunnerAssignments", "ModuleProbeAttempts", "SignalOutputSets", "ResourceChangeSets"
        ]
    };

    /// <summary>
    /// Every foreign key restricting on one of the four owners names a table that is cleared.
    /// A failure is either a missing clear or a table that no longer needs one.
    /// </summary>
    [Fact]
    public void Every_Restricting_Reference_To_A_Deletable_Owner_Is_Cleared()
    {
        var uncovered = new List<string>();

        foreach (var (owner, cleared) in Cleared)
        {
            foreach (var foreignKey in RestrictingReferencesTo(owner))
            {
                var table = foreignKey.DeclaringEntityType.GetTableName();
                if (table == null || cleared.Contains(table)) continue;

                uncovered.Add(
                    $"{table}.{string.Join(",", foreignKey.Properties.Select(p => p.Name))} " +
                    $"restricts on {owner.Name} and nothing clears it");
            }
        }

        Assert.Empty(uncovered.Distinct().OrderBy(line => line));
    }

    /// <summary>
    /// The reverse: a table listed as cleared still has a reason to be. A clear left behind after
    /// its foreign key changed is dead code that reads as load-bearing.
    /// </summary>
    [Fact]
    public void Every_Cleared_Table_Still_Restricts_On_Its_Owner()
    {
        var stale = new List<string>();

        foreach (var (owner, cleared) in Cleared)
        {
            var restricting = RestrictingReferencesTo(owner)
                .Select(fk => fk.DeclaringEntityType.GetTableName())
                .Where(name => name != null)
                .ToHashSet();

            // Cleared because something else in the owner's set restricts on it, not the owner.
            var indirect = Indirect.TryGetValue(owner, out var tables) ? tables : [];

            foreach (var table in cleared.Where(t => !restricting.Contains(t) && !indirect.Contains(t)))
                stale.Add($"{table} is cleared for {owner.Name} but no longer restricts on it");
        }

        Assert.Empty(stale.OrderBy(line => line));
    }

    /// <summary>
    /// Tables cleared not because they restrict on the owner, but because they restrict on a row
    /// that does: a secret's readers restrict on the secret, and a state migration job on the
    /// transfer it belongs to.
    /// </summary>
    private static readonly Dictionary<Type, string[]> Indirect = new()
    {
        [typeof(Module)] = ["StateMigrationJobs"],
        [typeof(Namespace)] = ["ModuleInputs"],
        [typeof(Stack)] = [],
        [typeof(ModuleJob)] = []
    };

    private static IEnumerable<IForeignKey> RestrictingReferencesTo(Type owner) =>
        Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => fk.PrincipalEntityType.ClrType == owner)
            .Where(fk => fk.DeleteBehavior is DeleteBehavior.Restrict or DeleteBehavior.NoAction);

    /// <summary>The model alone, which needs no database to build.</summary>
    private static readonly IModel Model = new ModelOnlyDbContext().Model;

    private sealed class ModelOnlyDbContext() : SnapCdDbContext(
        new DbContextOptionsBuilder<SnapCdDbContext>()
            .UseSqlServer("Server=unused;Database=unused")
            .Options);
}
