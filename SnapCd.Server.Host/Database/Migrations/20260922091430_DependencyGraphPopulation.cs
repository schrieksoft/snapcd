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
    /// Rebuilds the dependency graph's materialized tables from the live rows. Unconditional: a
    /// rebuild is also the repair for anything an older version left wrong.
    ///
    /// Runs after DependencyGraphObjects, which creates the procedures called here.
    /// </summary>
    public partial class DependencyGraphPopulation : Migration
    {
        private const string SqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20260922091430_DependencyGraphPopulation.sql";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // suppressTransaction: the unscoped rebuild and the scoped rebuilds its own triggers
            // fire take locks in conflicting orders, and sp_getapplock only serializes them when
            // @@TRANCOUNT > 0. Sharing EF's migration transaction deadlocks (error 1205).
            //
            // Not atomic as a result. Each procedure rebuilds from scratch, so re-running fixes a
            // partial run.
            foreach (var batch in MigrationSql.ReadBatches(SqlResource))
            {
                migrationBuilder.Sql(batch, suppressTransaction: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo: the tables hold no data of their own.
        }
    }
}
