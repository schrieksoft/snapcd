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
    public partial class AddManualModuleJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManualModuleJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimestampStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TimestampEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WaitingForApproval = table.Column<bool>(type: "bit", nullable: true),
                    ServerSideErrorHeader = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ServerSideError = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    Logs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualModuleJobs", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ManualModuleJobs_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManualModuleJobs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobs_Id",
                table: "ManualModuleJobs",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobs_ModuleId",
                table: "ManualModuleJobs",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobs_ModuleId_OrganizationId",
                table: "ManualModuleJobs",
                columns: new[] { "ModuleId", "OrganizationId" },
                unique: true,
                filter: "[Status] = 'Running'");

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobs_ModuleId_TimestampStart_OrganizationId",
                table: "ManualModuleJobs",
                columns: new[] { "ModuleId", "TimestampStart", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobs_OrganizationId",
                table: "ManualModuleJobs",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManualModuleJobs");
        }
    }
}
