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
    /// <inheritdoc />
    /// <summary>
    /// Gives ModuleState the identity of the two jobs its other columns are derived from, so a
    /// reader can join ModuleJobs for further job detail rather than the table growing a column
    /// per attribute.
    /// </summary>
    public partial class ModuleStateJobIds : Migration
    {
        private const string SqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261104162648_ModuleStateJobIds.sql";

        // The previous definitions of the procedure and the three triggers, reverted by re-running
        // the migration that created them. The columns are left in place: dropping them would
        // require rebuilding every object that names them, for no gain on a cache table.
        private const string DownSqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20260922091420_DependencyGraphObjects.sql";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // suppressTransaction: the rebuild at the end of the script takes locks in an order
            // that conflicts with the triggers it fires, and sp_getapplock only serializes them
            // when @@TRANCOUNT > 0. Sharing EF's migration transaction deadlocks (error 1205).
            //
            // Not atomic as a result. The procedure rebuilds from scratch, so re-running repairs
            // a partial run.
            foreach (var batch in MigrationSql.ReadBatches(SqlResource))
            {
                migrationBuilder.Sql(batch, suppressTransaction: true);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var batch in MigrationSql.ReadBatches(DownSqlResource))
            {
                migrationBuilder.Sql(batch, suppressTransaction: true);
            }
        }
    }
}
