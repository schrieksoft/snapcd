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
    public partial class ApprovalDecisionOnJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalDecision",
                table: "StateMigrationJobs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalThreshold",
                table: "StateMigrationJobs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalDecision",
                table: "ModuleJobs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalThreshold",
                table: "ModuleJobs",
                type: "int",
                nullable: true);

            // A decline is terminal, so a declined row decides the job however many approvals sit beside it.
            migrationBuilder.Sql(@"
UPDATE j SET j.[ApprovalDecision] = CASE WHEN EXISTS (
        SELECT 1 FROM [ModuleJobApprovals] d
        WHERE d.[ModuleJobId] = j.[Id] AND d.[OrganizationId] = j.[OrganizationId] AND d.[Declined] = 1)
    THEN 'Declined' ELSE 'Approved' END
FROM [ModuleJobs] j
WHERE EXISTS (SELECT 1 FROM [ModuleJobApprovals] a
              WHERE a.[ModuleJobId] = j.[Id] AND a.[OrganizationId] = j.[OrganizationId]);");

            // NotApproved with no approval row is consistent with a timeout and with anything else
            // that finalized through that path, so it records that the job waited and nothing more.
            // A job still waiting is caught here too: null would hide its history until someone decides.
            migrationBuilder.Sql(@"
UPDATE [ModuleJobs] SET [ApprovalDecision] = 'None'
WHERE ([Status] = 'NotApproved' OR [WaitingForApproval] = 1) AND [ApprovalDecision] IS NULL;");

            migrationBuilder.Sql(@"
UPDATE j SET j.[ApprovalDecision] = CASE WHEN EXISTS (
        SELECT 1 FROM [StateMigrationJobApprovals] d
        WHERE d.[StateMigrationJobId] = j.[Id] AND d.[OrganizationId] = j.[OrganizationId] AND d.[Declined] = 1)
    THEN 'Declined' ELSE 'Approved' END
FROM [StateMigrationJobs] j
WHERE EXISTS (SELECT 1 FROM [StateMigrationJobApprovals] a
              WHERE a.[StateMigrationJobId] = j.[Id] AND a.[OrganizationId] = j.[OrganizationId]);");

            migrationBuilder.Sql(@"
UPDATE [StateMigrationJobs] SET [ApprovalDecision] = 'None'
WHERE ([Status] = 'NotApproved' OR [WaitingForApproval] = 1) AND [ApprovalDecision] IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalDecision",
                table: "StateMigrationJobs");

            migrationBuilder.DropColumn(
                name: "ApprovalThreshold",
                table: "StateMigrationJobs");

            migrationBuilder.DropColumn(
                name: "ApprovalDecision",
                table: "ModuleJobs");

            migrationBuilder.DropColumn(
                name: "ApprovalThreshold",
                table: "ModuleJobs");
        }
    }
}
