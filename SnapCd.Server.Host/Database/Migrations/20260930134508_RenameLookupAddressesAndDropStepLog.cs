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
    public partial class RenameLookupAddressesAndDropStepLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A rename, not a drop and recreate: EF scaffolds the latter for a renamed entity, which
            // would take any rows with it.
            migrationBuilder.RenameTable(
                name: "StateListFilteredSagas",
                newName: "LookupAddressesSagas");

            migrationBuilder.DropColumn(
                name: "Log",
                table: "StateMigrationJobSteps");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "LookupAddressesSagas",
                newName: "StateListFilteredSagas");

            migrationBuilder.AddColumn<string>(
                name: "Log",
                table: "StateMigrationJobSteps",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}