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
    public partial class TransfersAndHolds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "HeldAt",
                table: "ModuleSagas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HeldByTransferId",
                table: "ModuleSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ManualModuleJobSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Task = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Attempt = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExitCode = table.Column<int>(type: "int", nullable: true),
                    ErrorHeader = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    RunnerInstanceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    InputKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Log = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_ManualModuleJobSteps", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ManualModuleJobSteps_ManualModuleJobs_JobId_OrganizationId",
                        columns: x => new { x.JobId, x.OrganizationId },
                        principalTable: "ManualModuleJobs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManualModuleJobSteps_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiverModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MapJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MapHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CloseReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceProveRef = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SourceLockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SourceLockedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceMergedDeclaredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SourceMergedDeclaredBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceMergedCommit = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceReleasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReceiverProveRef = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ReceiverConsentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReceiverConsentDecidedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiverConsentDecidedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReceiverConsentDecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReceiverConsentReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReceiverLockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReceiverLockedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiverMergedDeclaredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReceiverMergedDeclaredBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiverMergedCommit = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReceiverReleasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_Transfers", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_Transfers_Modules_ReceiverModuleId_OrganizationId",
                        columns: x => new { x.ReceiverModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transfers_Modules_SourceModuleId_OrganizationId",
                        columns: x => new { x.SourceModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transfers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobSteps_Id",
                table: "ManualModuleJobSteps",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobSteps_JobId_ModuleId_Task_Attempt_OrganizationId",
                table: "ManualModuleJobSteps",
                columns: new[] { "JobId", "ModuleId", "Task", "Attempt", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobSteps_JobId_OrganizationId",
                table: "ManualModuleJobSteps",
                columns: new[] { "JobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobSteps_ModuleId",
                table: "ManualModuleJobSteps",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobSteps_OrganizationId",
                table: "ManualModuleJobSteps",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobSteps_TransferId",
                table: "ManualModuleJobSteps",
                column: "TransferId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_Id",
                table: "Transfers",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_MapHash",
                table: "Transfers",
                column: "MapHash");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_OrganizationId",
                table: "Transfers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_ReceiverModuleId",
                table: "Transfers",
                column: "ReceiverModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_ReceiverModuleId_OrganizationId",
                table: "Transfers",
                columns: new[] { "ReceiverModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_SourceModuleId",
                table: "Transfers",
                column: "SourceModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_SourceModuleId_OrganizationId",
                table: "Transfers",
                columns: new[] { "SourceModuleId", "OrganizationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManualModuleJobSteps");

            migrationBuilder.DropTable(
                name: "Transfers");

            migrationBuilder.DropColumn(
                name: "HeldAt",
                table: "ModuleSagas");

            migrationBuilder.DropColumn(
                name: "HeldByTransferId",
                table: "ModuleSagas");
        }
    }
}
