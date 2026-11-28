// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore.Migrations;
using SnapCd.Server.Host.Database.Migrations.Sql;

#nullable disable

namespace SnapCd.Server.Host.Database.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Gives each Stack a root Namespace with the Stack's own id, places the Stack's Namespaces
    /// under it, and gives it the Stack's roles, supplies, missions, integration events and Secrets.
    /// </summary>
    public partial class StacksBecomeRootNamespaces : Migration
    {
        private const string SqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261128174321_StacksBecomeRootNamespaces.sql";

        private const string DownSqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261128174321_StacksBecomeRootNamespaces.Down.sql";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Dropped first: a root may carry the name of a Namespace in its own Stack.
            migrationBuilder.DropIndex(
                name: "IX_Namespaces_OrganizationId_StackId_Name",
                table: "Namespaces");

            foreach (var batch in MigrationSql.ReadBatches(SqlResource))
            {
                migrationBuilder.Sql(batch);
            }

            migrationBuilder.CreateIndex(
                name: "IX_Namespaces_OrganizationId_StackId_ParentNamespaceId_Name",
                table: "Namespaces",
                columns: new[] { "OrganizationId", "StackId", "ParentNamespaceId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Namespaces_OrganizationId_StackId_ParentNamespaceId_Name",
                table: "Namespaces");

            foreach (var batch in MigrationSql.ReadBatches(DownSqlResource))
            {
                migrationBuilder.Sql(batch);
            }

            migrationBuilder.CreateIndex(
                name: "IX_Namespaces_OrganizationId_StackId_Name",
                table: "Namespaces",
                columns: new[] { "OrganizationId", "StackId", "Name" },
                unique: true);
        }
    }
}
