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
    /// Derives both metadata reader sets for the rows already in the database, which no trigger
    /// fires for.
    ///
    /// Separate from the migration that creates the procedures: a table type cannot be declared in
    /// the transaction that created it, and the backfill passes one to each procedure.
    /// </summary>
    public partial class DerivedRoleAssignmentBackfill : Migration
    {
        private const string SqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261007181718_DerivedRoleAssignmentBackfill.sql";

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
            migrationBuilder.Sql("DELETE FROM dbo.DerivedOrganizationRoleAssignments;");
            migrationBuilder.Sql("DELETE FROM dbo.DerivedRunnerRoleAssignments;");
            migrationBuilder.Sql("DELETE FROM dbo.DerivedAgentRoleAssignments;");
            migrationBuilder.Sql("DELETE FROM dbo.DerivedIntegrationRoleAssignments;");
        }
    }
}
