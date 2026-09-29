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
    public partial class RemoveGracefulCancellation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GracefulCancellationRequestId",
                table: "TransferMigrateSagas");

            migrationBuilder.DropColumn(
                name: "GracefulCancellationRequestId",
                table: "StateListFilteredSagas");

            migrationBuilder.DropColumn(
                name: "GracefulCancellationRequestId",
                table: "SplitMigrateSagas");

            migrationBuilder.DropColumn(
                name: "GracefulCancellationRequestId",
                table: "RemoveSagas");

            migrationBuilder.DropColumn(
                name: "GracefulCancellationRequestId",
                table: "MoveSagas");

            migrationBuilder.DropColumn(
                name: "GracefulCancellationRequestId",
                table: "ImportSagas");

            migrationBuilder.DropColumn(
                name: "GracefulCancellationRequestId",
                table: "DestroyJobSagas");

            migrationBuilder.DropColumn(
                name: "GracefulCancellationRequestId",
                table: "ApplyJobSagas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GracefulCancellationRequestId",
                table: "TransferMigrateSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GracefulCancellationRequestId",
                table: "StateListFilteredSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GracefulCancellationRequestId",
                table: "SplitMigrateSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GracefulCancellationRequestId",
                table: "RemoveSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GracefulCancellationRequestId",
                table: "MoveSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GracefulCancellationRequestId",
                table: "ImportSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GracefulCancellationRequestId",
                table: "DestroyJobSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GracefulCancellationRequestId",
                table: "ApplyJobSagas",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
