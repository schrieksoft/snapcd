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
    /// The procedure and triggers that keep RecursiveGroupMembers current.
    ///
    /// The rebuild is ordered First on GroupMembers, ahead of the derivation triggers that read
    /// the closure it maintains.
    /// </summary>
    public partial class RecursiveGroupMemberTriggers : Migration
    {
        private const string SqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261007174143_RecursiveGroupMemberTriggers.sql";

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
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.trg_GroupMembers_RecursiveRebuild;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS dbo.trg_Groups_RecursiveRebuild;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_RebuildRecursiveGroupMembers;");
        }
    }
}
