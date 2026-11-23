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
    public partial class NamespaceSupplyInheritance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InheritedByDescendants",
                table: "RunnerNamespaceSupplies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "InheritedByDescendants",
                table: "OutputSetNamespaceSupplies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "InheritedByDescendants",
                table: "OutputNamespaceSupplies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "InheritedByDescendants",
                table: "IntegrationNamespaceSupplies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "InheritedByDescendants",
                table: "AgentNamespaceSupplies",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InheritedByDescendants",
                table: "RunnerNamespaceSupplies");

            migrationBuilder.DropColumn(
                name: "InheritedByDescendants",
                table: "OutputSetNamespaceSupplies");

            migrationBuilder.DropColumn(
                name: "InheritedByDescendants",
                table: "OutputNamespaceSupplies");

            migrationBuilder.DropColumn(
                name: "InheritedByDescendants",
                table: "IntegrationNamespaceSupplies");

            migrationBuilder.DropColumn(
                name: "InheritedByDescendants",
                table: "AgentNamespaceSupplies");
        }
    }
}
