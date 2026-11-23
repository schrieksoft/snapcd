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
    /// Replaces the ancestry closure's trigger and procedure, so that a writer touches only the
    /// rows of the Namespaces it is writing and creates are kept apart from moves by a lock.
    /// </summary>
    public partial class NamespaceAncestorWriters : Migration
    {
        private const string SqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261123131045_NamespaceAncestorWriters.sql";

        private const string ReplacedSqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261123110710_ModuleResolvedSettings.sql";

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
            foreach (var batch in MigrationSql.ReadBatches(ReplacedSqlResource))
            {
                migrationBuilder.Sql(batch);
            }
        }
    }
}
