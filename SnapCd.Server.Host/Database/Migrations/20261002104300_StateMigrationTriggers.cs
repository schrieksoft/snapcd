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
    /// The three triggers a transfer needs, each in its final form.
    ///
    /// trg_Transfers_TransferLocks claims both Modules when a transfer opens and releases them when
    /// it closes, so the claim and the release are made by the statement that caused them.
    ///
    /// trg_StateMigrationJobs_CloseTransfer closes a transfer as soon as any of its jobs ends.
    ///
    /// trg_StateMigrationJobs_DeleteTransferArtefacts deletes what the two halves carried once
    /// neither is still running, which is later than the close: the surviving half still needs what
    /// the first produced.
    /// </summary>
    public partial class StateMigrationTriggers : Migration
    {
        private const string SqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261002104300_StateMigrationTriggers.sql";

        private static readonly string[] Triggers =
        [
            "trg_Transfers_TransferLocks",
            "trg_StateMigrationJobs_CloseTransfer",
            "trg_StateMigrationJobs_DeleteTransferArtefacts"
        ];

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
            foreach (var trigger in Triggers)
            {
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {trigger};");
            }
        }
    }
}
