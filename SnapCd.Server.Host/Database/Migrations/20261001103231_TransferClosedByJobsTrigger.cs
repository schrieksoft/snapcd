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
    /// Deploys the trigger that closes a transfer once neither of its jobs is still running.
    ///
    /// A transfer claims both its Modules in the statement that creates it. Releasing them went
    /// through a published event and a consumer, so a Module could stay locked for as long as it
    /// took anyone to notice a message that never arrived. The claim and the release are now the
    /// same kind of thing: both happen in the statement that caused them.
    /// </summary>
    public partial class TransferClosedByJobsTrigger : Migration
    {
        private const string SqlResource =
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
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_StateMigrationJobs_CloseTransfer;");
        }
    }
}
