// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnapCd.Server.Host.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenancePhase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Phase",
                table: "MaintenanceMode",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhaseActionCompletedAt",
                table: "MaintenanceMode",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhaseActionSummary",
                table: "MaintenanceMode",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PhaseEnteredAt",
                table: "MaintenanceMode",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkippedPhases",
                table: "MaintenanceMode",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Phase",
                table: "MaintenanceMode");

            migrationBuilder.DropColumn(
                name: "PhaseActionCompletedAt",
                table: "MaintenanceMode");

            migrationBuilder.DropColumn(
                name: "PhaseActionSummary",
                table: "MaintenanceMode");

            migrationBuilder.DropColumn(
                name: "PhaseEnteredAt",
                table: "MaintenanceMode");

            migrationBuilder.DropColumn(
                name: "SkippedPhases",
                table: "MaintenanceMode");
        }
    }
}
