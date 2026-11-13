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
    public partial class NamespaceInheritanceControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IgnoreNamespaceHooks",
                table: "Modules",
                newName: "IgnoreInheritedHooks");

            migrationBuilder.RenameColumn(
                name: "IgnoreNamespaceFlags",
                table: "Modules",
                newName: "IgnoreInheritedFlags");

            migrationBuilder.RenameColumn(
                name: "IgnoreNamespaceExtraFiles",
                table: "Modules",
                newName: "IgnoreInheritedExtraFiles");

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreInheritedExtraFiles",
                table: "Namespaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreInheritedFlags",
                table: "Namespaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreInheritedHooks",
                table: "Namespaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreInheritedIntegrationEvents",
                table: "Namespaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreInheritedMissions",
                table: "Namespaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreInheritedPolicies",
                table: "Namespaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreInheritedTriggerPaths",
                table: "Namespaces",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreInheritedPolicies",
                table: "Modules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreInheritedMissions",
                table: "Modules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreInheritedTriggerPaths",
                table: "Modules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreInheritedIntegrationEvents",
                table: "Modules",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IgnoreInheritedExtraFiles",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "IgnoreInheritedFlags",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "IgnoreInheritedHooks",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "IgnoreInheritedIntegrationEvents",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "IgnoreInheritedMissions",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "IgnoreInheritedPolicies",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "IgnoreInheritedTriggerPaths",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "IgnoreInheritedExtraFiles",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "IgnoreInheritedFlags",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "IgnoreInheritedHooks",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "IgnoreInheritedIntegrationEvents",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "IgnoreInheritedPolicies",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "IgnoreInheritedMissions",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "IgnoreInheritedTriggerPaths",
                table: "Modules");

            migrationBuilder.RenameColumn(
                name: "IgnoreInheritedTriggerPaths",
                table: "Modules",
                newName: "IgnoreNamespaceHooks");

            migrationBuilder.RenameColumn(
                name: "IgnoreInheritedPolicies",
                table: "Modules",
                newName: "IgnoreNamespaceFlags");

            migrationBuilder.RenameColumn(
                name: "IgnoreInheritedMissions",
                table: "Modules",
                newName: "IgnoreNamespaceExtraFiles");
        }
    }
}
