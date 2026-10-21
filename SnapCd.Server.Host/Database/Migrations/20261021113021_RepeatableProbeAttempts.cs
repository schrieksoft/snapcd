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
    public partial class RepeatableProbeAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ModuleProbeAttempts_ProbeWaveId_ModuleId_OrganizationId",
                table: "ModuleProbeAttempts");

            migrationBuilder.AddColumn<long>(
                name: "Timestamp",
                table: "ModuleProbeAttempts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleProbeAttempts_ProbeWaveId_ModuleId_OrganizationId_Timestamp",
                table: "ModuleProbeAttempts",
                columns: new[] { "ProbeWaveId", "ModuleId", "OrganizationId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ModuleProbeAttempts_ProbeWaveId_ModuleId_OrganizationId_Timestamp",
                table: "ModuleProbeAttempts");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "ModuleProbeAttempts");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleProbeAttempts_ProbeWaveId_ModuleId_OrganizationId",
                table: "ModuleProbeAttempts",
                columns: new[] { "ProbeWaveId", "ModuleId", "OrganizationId" },
                unique: true);
        }
    }
}
