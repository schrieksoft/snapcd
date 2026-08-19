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
    public partial class AddSplitMonolithJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManualModuleJobApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManualModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DecisionDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Declined = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ManualModuleJobApprovals", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ManualModuleJobApprovals_ManualModuleJobs_ManualModuleJobId_OrganizationId",
                        columns: x => new { x.ManualModuleJobId, x.OrganizationId },
                        principalTable: "ManualModuleJobs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManualModuleJobApprovals_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SplitMonolithSagas",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutDirectory = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RootDirectory = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Overwrite = table.Column<bool>(type: "bit", nullable: false),
                    RefactorMapHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CarvedModuleNames = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ProvenModuleCount = table.Column<int>(type: "int", nullable: true),
                    NegativeVerdict = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ResponseAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GracefulCancellationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    KillCancellationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HeartbeatRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HeartbeatScheduleTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalTimeoutScheduleTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovalTimeoutMinutes = table.Column<int>(type: "int", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RunnerInstanceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DeclaredJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsDeclined = table.Column<bool>(type: "bit", nullable: false),
                    PreviousStateBeforeWaiting = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PreviousStateBeforeCancelling = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    WaitingSince = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServerInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefinitiveRevision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SplitMonolithSagas", x => new { x.CorrelationId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_SplitMonolithSagas_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobApprovals_Id",
                table: "ManualModuleJobApprovals",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobApprovals_ManualModuleJobId",
                table: "ManualModuleJobApprovals",
                column: "ManualModuleJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobApprovals_ManualModuleJobId_OrganizationId",
                table: "ManualModuleJobApprovals",
                columns: new[] { "ManualModuleJobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobApprovals_ManualModuleJobId_PrincipalId_OrganizationId",
                table: "ManualModuleJobApprovals",
                columns: new[] { "ManualModuleJobId", "PrincipalId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobApprovals_OrganizationId",
                table: "ManualModuleJobApprovals",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobApprovals_PrincipalId",
                table: "ManualModuleJobApprovals",
                column: "PrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_SplitMonolithSagas_CorrelationId",
                table: "SplitMonolithSagas",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SplitMonolithSagas_ModuleId_OrganizationId",
                table: "SplitMonolithSagas",
                columns: new[] { "ModuleId", "OrganizationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManualModuleJobApprovals");

            migrationBuilder.DropTable(
                name: "SplitMonolithSagas");
        }
    }
}
