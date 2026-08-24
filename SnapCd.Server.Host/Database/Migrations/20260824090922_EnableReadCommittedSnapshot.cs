// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnapCd.Server.Host.Database.Migrations
{
    /// <inheritdoc />
    public partial class EnableReadCommittedSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The dependency-graph triggers reconcile a module pair by re-reading every ModuleInput
            // for it. Under locking read-committed that read takes shared locks on rows a concurrent
            // writer holds exclusively, so two sessions creating inputs for one pair deadlock.
            // ALTER DATABASE cannot run inside the migration transaction, hence suppressTransaction.
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.databases
                           WHERE name = DB_NAME() AND is_read_committed_snapshot_on = 0)
                BEGIN
                    DECLARE @sql NVARCHAR(MAX) = N'ALTER DATABASE ' + QUOTENAME(DB_NAME())
                        + N' SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;';
                    EXEC sp_executesql @sql;
                END;
                """,
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately not reverted: the graph triggers deadlock without it.
        }
    }
}
