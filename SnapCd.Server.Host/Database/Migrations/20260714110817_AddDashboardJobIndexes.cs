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
    public partial class AddDashboardJobIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ModuleJobs_OrganizationId",
                table: "ModuleJobs");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobs_Organization_Activity",
                table: "ModuleJobs",
                columns: new[] { "OrganizationId", "TimestampStart" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "ModuleId", "JobNumber", "JobType", "Status", "WaitingForApproval", "TimestampEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobs_PendingApprovals",
                table: "ModuleJobs",
                columns: new[] { "OrganizationId", "TimestampStart" },
                filter: "[WaitingForApproval] = 1")
                .Annotation("SqlServer:Include", new[] { "ModuleId", "JobNumber", "JobType", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ModuleJobs_Organization_Activity",
                table: "ModuleJobs");

            migrationBuilder.DropIndex(
                name: "IX_ModuleJobs_PendingApprovals",
                table: "ModuleJobs");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobs_OrganizationId",
                table: "ModuleJobs",
                column: "OrganizationId");
        }
    }
}
