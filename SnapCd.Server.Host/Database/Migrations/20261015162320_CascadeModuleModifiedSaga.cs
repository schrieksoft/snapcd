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
    public partial class CascadeModuleModifiedSaga : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The new foreign key points from the saga at Modules, so a row whose Module is gone
            // would violate it. Those rows exist precisely because of the direction being fixed
            // here: nothing deleted a debouncer when its Module went. An orphaned debouncer is
            // per-Module state for a Module that does not exist, so it carries nothing.
            migrationBuilder.Sql(
                """
                DELETE s
                FROM dbo.ModuleModifiedSaga s
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM dbo.Modules m
                    WHERE m.Id = s.CorrelationId
                      AND m.OrganizationId = s.OrganizationId);
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Modules_ModuleModifiedSaga_Id_OrganizationId",
                table: "Modules");

            migrationBuilder.AddForeignKey(
                name: "FK_ModuleModifiedSaga_Modules_CorrelationId_OrganizationId",
                table: "ModuleModifiedSaga",
                columns: new[] { "CorrelationId", "OrganizationId" },
                principalTable: "Modules",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModuleModifiedSaga_Modules_CorrelationId_OrganizationId",
                table: "ModuleModifiedSaga");

            migrationBuilder.AddForeignKey(
                name: "FK_Modules_ModuleModifiedSaga_Id_OrganizationId",
                table: "Modules",
                columns: new[] { "Id", "OrganizationId" },
                principalTable: "ModuleModifiedSaga",
                principalColumns: new[] { "CorrelationId", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
