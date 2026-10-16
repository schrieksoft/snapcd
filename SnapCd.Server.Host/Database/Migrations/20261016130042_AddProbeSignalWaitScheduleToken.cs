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
    public partial class AddProbeSignalWaitScheduleToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "TransferMigrateSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "SplitMigrateSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "RemoveSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "MoveSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "LookupAddressesSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "ImportSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "DestroyJobSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "ApplyJobSagas",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "TransferMigrateSagas");

            migrationBuilder.DropColumn(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "SplitMigrateSagas");

            migrationBuilder.DropColumn(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "RemoveSagas");

            migrationBuilder.DropColumn(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "MoveSagas");

            migrationBuilder.DropColumn(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "LookupAddressesSagas");

            migrationBuilder.DropColumn(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "ImportSagas");

            migrationBuilder.DropColumn(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "DestroyJobSagas");

            migrationBuilder.DropColumn(
                name: "ProbeSignalWaitScheduleTokenId",
                table: "ApplyJobSagas");
        }
    }
}
