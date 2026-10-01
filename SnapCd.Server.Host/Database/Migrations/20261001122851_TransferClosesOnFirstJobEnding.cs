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
    /// Closes a transfer when any of its jobs ends, rather than when both have.
    ///
    /// Waiting for both meant the close depended on reading a job row another transaction owns, and
    /// a transfer that missed its close left both Modules showing a greyed-out transfer button with
    /// no way back short of editing the database. A transfer does not keep a Module free for its
    /// counterparty - a Module already running a state migration refuses the next one itself - so
    /// there is nothing to hold open once a side has ended.
    /// </summary>
    public partial class TransferClosesOnFirstJobEnding : Migration
    {
        private const string SqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261001122851_TransferClosesOnFirstJobEnding.sql";

        /// <summary>The previous body, replayed so a rollback restores it rather than dropping it.</summary>
        private const string PreviousSqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261001103231_TransferClosedByJobsTrigger.sql";

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
            foreach (var batch in MigrationSql.ReadBatches(PreviousSqlResource))
            {
                migrationBuilder.Sql(batch);
            }
        }
    }
}
