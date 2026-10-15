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
    public partial class RemoveRippleProbeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RippleProbeSagas");

            migrationBuilder.DropTable(
                name: "RunnerConnectionRippleProbes");

            migrationBuilder.DropTable(
                name: "RippleProbeJobs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RippleProbeJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpstreamModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FailedOnServerSideStep = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    InputsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JobNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Logs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SelectedRunnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServerSideError = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    ServerSideErrorHeader = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SignalsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TimestampEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TimestampStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpstreamModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WaitingForRunner = table.Column<bool>(type: "bit", nullable: true)
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
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalTimeoutMinutes = table.Column<int>(type: "int", nullable: true),
                    ApprovalTimeoutScheduleTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DeclaredJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefinitiveRevision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    HeartbeatRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HeartbeatScheduleTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InputsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    IsDeclined = table.Column<bool>(type: "bit", nullable: false),
                    KillCancellationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStateBeforeCancelling = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PreviousStateBeforeWaiting = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponseAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    RunnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerInstanceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RunnerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ServerInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SignalsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpstreamModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpstreamModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    WaitingSince = table.Column<DateTime>(type: "datetime2", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "RunnerConnectionRippleProbes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RippleProbeJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaskName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunnerConnectionRippleProbes", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_RunnerConnectionRippleProbes_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RunnerConnectionRippleProbes_RippleProbeJobs_RippleProbeJobId_OrganizationId",
                        columns: x => new { x.RippleProbeJobId, x.OrganizationId },
                        principalTable: "RippleProbeJobs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RunnerConnectionRippleProbes_RunnerConnections_RunnerConnectionId_OrganizationId",
                        columns: x => new { x.RunnerConnectionId, x.OrganizationId },
                        principalTable: "RunnerConnections",
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
                columns: new[] { "ModuleId", "OrganizationId" },
                unique: true,
                filter: "[Status] = 'Running'");

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

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionRippleProbes_Id",
                table: "RunnerConnectionRippleProbes",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionRippleProbes_OrganizationId",
                table: "RunnerConnectionRippleProbes",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionRippleProbes_RippleProbeJobId_OrganizationId",
                table: "RunnerConnectionRippleProbes",
                columns: new[] { "RippleProbeJobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionRippleProbes_RunnerConnectionId_OrganizationId",
                table: "RunnerConnectionRippleProbes",
                columns: new[] { "RunnerConnectionId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionRippleProbes_RunnerConnectionId_RippleProbeJobId_OrganizationId",
                table: "RunnerConnectionRippleProbes",
                columns: new[] { "RunnerConnectionId", "RippleProbeJobId", "OrganizationId" },
                unique: true);
        }
    }
}
