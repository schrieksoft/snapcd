// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore.Migrations;
using SnapCd.Server.Host.Database.Migrations.Sql;

#nullable disable

namespace SnapCd.Server.Host.Database.Migrations
{
    /// <summary>
    /// Deploys the dependency graph's table types, procedures and triggers from the SQL file of
    /// the same name.
    ///
    /// These were idempotent startup SQL (01_DependencyGraph.sql), re-run on every boot. They are
    /// a migration because the pieces have a strict order - types, then the procedures, then the
    /// triggers that call them - which only a migration guarantees.
    ///
    /// Filling the tables afterwards stays in startup SQL (01_DependencyGraphPopulation.sql): it
    /// is guarded on the tables being empty rather than on a version, and
    /// sp_RecomputeRecursiveDependencyEdges deadlocks against its own triggers when called inside
    /// the transaction EF wraps a migration in.
    ///
    /// The SQL file beside this one is a snapshot, not a live definition: editing it changes
    /// nothing on a database that has already run this migration. To change an object, add a new
    /// migration with its own SQL file. The current definition of any object is the one in the
    /// newest migration that touched it.
    /// </summary>
    public partial class DependencyGraphObjects : Migration
    {
        private const string SqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20260922091420_DependencyGraphObjects.sql";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var batch in MigrationSql.ReadBatches(SqlResource))
            {
                migrationBuilder.Sql(batch);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The tables the triggers maintain are left alone.
            foreach (var trigger in new[]
                     {
                         "trg_DependsOnModules_DependencyEdges",
                         "trg_ModuleInputs_DependencyEdges",
                         "trg_DependencyEdges_RecursiveClosure",
                         "trg_Namespaces_ClosureNames",
                         "trg_Stacks_ClosureNames",
                         "trg_Modules_ClosureNames",
                         "trg_ModuleJobs_ModuleState",
                         "trg_ModuleSagas_ModuleState",
                         "trg_Modules_ModuleState",
                     })
            {
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {trigger};");
            }

            foreach (var procedure in new[]
                     {
                         "sp_RecomputeDependencyEdges",
                         "sp_UpdateDependencyEdgesForModules",
                         "sp_RecomputeRecursiveDependencyEdges",
                         "sp_RefreshClosureDisplayNames",
                         "sp_RecomputeModuleState",
                     })
            {
                migrationBuilder.Sql($"DROP PROCEDURE IF EXISTS {procedure};");
            }
        }
    }
}
