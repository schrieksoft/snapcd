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
    public partial class ProbeWaveBelongsToItsOriginJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProbeWaves_Modules_OriginModuleId_OrganizationId",
                table: "ProbeWaves");

            migrationBuilder.DropIndex(
                name: "IX_ProbeWaves_OriginModuleId_OrganizationId",
                table: "ProbeWaves");

            migrationBuilder.DropColumn(
                name: "OriginModuleId",
                table: "ProbeWaves");

            migrationBuilder.AddForeignKey(
                name: "FK_ProbeWaves_ModuleJobs_OriginModuleJobId_OrganizationId",
                table: "ProbeWaves",
                columns: new[] { "OriginModuleJobId", "OrganizationId" },
                principalTable: "ModuleJobs",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProbeWaves_ModuleJobs_OriginModuleJobId_OrganizationId",
                table: "ProbeWaves");

            migrationBuilder.AddColumn<Guid>(
                name: "OriginModuleId",
                table: "ProbeWaves",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ProbeWaves_OriginModuleId_OrganizationId",
                table: "ProbeWaves",
                columns: new[] { "OriginModuleId", "OrganizationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProbeWaves_Modules_OriginModuleId_OrganizationId",
                table: "ProbeWaves",
                columns: new[] { "OriginModuleId", "OrganizationId" },
                principalTable: "Modules",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
