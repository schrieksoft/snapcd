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
    public partial class SecretScopeKindFromDiscriminator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ScopeKind",
                table: "Secrets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                computedColumnSql: "CASE [Discriminator] WHEN N'NamespaceSecret' THEN N'Namespace' WHEN N'ModuleSecret' THEN N'Module' ELSE N'Stack' END",
                stored: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Added nullable and filled before it is required: the table holds rows.
            migrationBuilder.Sql("ALTER TABLE dbo.Secrets DROP COLUMN ScopeKind");
            migrationBuilder.Sql("ALTER TABLE dbo.Secrets ADD ScopeKind nvarchar(max) NULL");
            migrationBuilder.Sql(
                "UPDATE dbo.Secrets SET ScopeKind = CASE [Discriminator] WHEN N'NamespaceSecret' THEN N'Namespace' WHEN N'ModuleSecret' THEN N'Module' ELSE N'Stack' END");
            migrationBuilder.Sql("ALTER TABLE dbo.Secrets ALTER COLUMN ScopeKind nvarchar(max) NOT NULL");
        }
    }
}
