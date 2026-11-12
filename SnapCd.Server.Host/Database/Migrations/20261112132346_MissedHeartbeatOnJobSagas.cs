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
    public partial class MissedHeartbeatOnJobSagas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MissedHeartbeat",
                table: "TransferMigrateSagas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MissedHeartbeat",
                table: "SplitMigrateSagas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MissedHeartbeat",
                table: "RemoveSagas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MissedHeartbeat",
                table: "MoveSagas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MissedHeartbeat",
                table: "LookupAddressesSagas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MissedHeartbeat",
                table: "ImportSagas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MissedHeartbeat",
                table: "DestroyJobSagas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MissedHeartbeat",
                table: "ApplyJobSagas",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MissedHeartbeat",
                table: "TransferMigrateSagas");

            migrationBuilder.DropColumn(
                name: "MissedHeartbeat",
                table: "SplitMigrateSagas");

            migrationBuilder.DropColumn(
                name: "MissedHeartbeat",
                table: "RemoveSagas");

            migrationBuilder.DropColumn(
                name: "MissedHeartbeat",
                table: "MoveSagas");

            migrationBuilder.DropColumn(
                name: "MissedHeartbeat",
                table: "LookupAddressesSagas");

            migrationBuilder.DropColumn(
                name: "MissedHeartbeat",
                table: "ImportSagas");

            migrationBuilder.DropColumn(
                name: "MissedHeartbeat",
                table: "DestroyJobSagas");

            migrationBuilder.DropColumn(
                name: "MissedHeartbeat",
                table: "ApplyJobSagas");
        }
    }
}
