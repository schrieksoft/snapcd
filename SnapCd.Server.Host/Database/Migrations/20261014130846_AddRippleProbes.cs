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
    public partial class AddRippleProbes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RippleProbeJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpstreamModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpstreamModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimestampStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TimestampEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InputsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SignalsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WaitingForRunner = table.Column<bool>(type: "bit", nullable: true),
                    FailedOnServerSideStep = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_RippleProbeJobs", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_RippleProbeJobs_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RippleProbeJobs_Modules_UpstreamModuleId_OrganizationId",
                        columns: x => new { x.UpstreamModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" });
                    table.ForeignKey(
                        name: "FK_RippleProbeJobs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RippleProbeSagas",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpstreamModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpstreamModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InputsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ResponseAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_RippleProbeSagas", x => new { x.CorrelationId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_RippleProbeSagas_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RippleProbeJobs_Id",
                table: "RippleProbeJobs",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RippleProbeJobs_ModuleId",
                table: "RippleProbeJobs",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RippleProbeJobs_ModuleId_OrganizationId",
                table: "RippleProbeJobs",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RippleProbeJobs_ModuleId_TimestampStart_OrganizationId",
                table: "RippleProbeJobs",
                columns: new[] { "ModuleId", "TimestampStart", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RippleProbeJobs_OrganizationId",
                table: "RippleProbeJobs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RippleProbeJobs_UpstreamModuleId_OrganizationId",
                table: "RippleProbeJobs",
                columns: new[] { "UpstreamModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RippleProbeJobs_UpstreamModuleJobId_OrganizationId",
                table: "RippleProbeJobs",
                columns: new[] { "UpstreamModuleJobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RippleProbeSagas_CorrelationId",
                table: "RippleProbeSagas",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RippleProbeSagas_ModuleId_OrganizationId",
                table: "RippleProbeSagas",
                columns: new[] { "ModuleId", "OrganizationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RippleProbeJobs");

            migrationBuilder.DropTable(
                name: "RippleProbeSagas");
        }
    }
}
