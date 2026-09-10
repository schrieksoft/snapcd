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
    public partial class MaintenanceModeSingleton : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: databases that already ran the service's lazy create keep their row.
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM [MaintenanceMode] WHERE [Id] = 1) INSERT INTO [MaintenanceMode] ([Id], [Enabled]) VALUES (1, 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MaintenanceMode_Singleton",
                table: "MaintenanceMode",
                sql: "[Id] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MaintenanceMode_Singleton",
                table: "MaintenanceMode");
        }
    }
}
