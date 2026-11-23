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
    public partial class InputKindFromDiscriminator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "InputKind",
                table: "NamespaceInputs",
                type: "nvarchar(450)",
                nullable: false,
                computedColumnSql: "CASE WHEN [Discriminator] LIKE N'%EnvVar%' THEN N'EnvVar' ELSE N'Param' END",
                stored: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "InputKind",
                table: "ModuleInputs",
                type: "nvarchar(450)",
                nullable: false,
                computedColumnSql: "CASE WHEN [Discriminator] LIKE N'%EnvVar%' THEN N'EnvVar' ELSE N'Param' END",
                stored: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "InputKind",
                table: "NamespaceInputs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldComputedColumnSql: "CASE WHEN [Discriminator] LIKE N'%EnvVar%' THEN N'EnvVar' ELSE N'Param' END");

            migrationBuilder.AlterColumn<string>(
                name: "InputKind",
                table: "ModuleInputs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldComputedColumnSql: "CASE WHEN [Discriminator] LIKE N'%EnvVar%' THEN N'EnvVar' ELSE N'Param' END");
        }
    }
}
