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
    public partial class RunnerConnectionManualJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RunnerConnectionManualJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManualModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
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
                    table.PrimaryKey("PK_RunnerConnectionManualJobs", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_RunnerConnectionManualJobs_ManualModuleJobs_ManualModuleJobId_OrganizationId",
                        columns: x => new { x.ManualModuleJobId, x.OrganizationId },
                        principalTable: "ManualModuleJobs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RunnerConnectionManualJobs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RunnerConnectionManualJobs_RunnerConnections_RunnerConnectionId_OrganizationId",
                        columns: x => new { x.RunnerConnectionId, x.OrganizationId },
                        principalTable: "RunnerConnections",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionManualJobs_Id",
                table: "RunnerConnectionManualJobs",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionManualJobs_ManualModuleJobId_OrganizationId",
                table: "RunnerConnectionManualJobs",
                columns: new[] { "ManualModuleJobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionManualJobs_OrganizationId",
                table: "RunnerConnectionManualJobs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionManualJobs_RunnerConnectionId_ManualModuleJobId_OrganizationId",
                table: "RunnerConnectionManualJobs",
                columns: new[] { "RunnerConnectionId", "ManualModuleJobId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionManualJobs_RunnerConnectionId_OrganizationId",
                table: "RunnerConnectionManualJobs",
                columns: new[] { "RunnerConnectionId", "OrganizationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunnerConnectionManualJobs");
        }
    }
}
