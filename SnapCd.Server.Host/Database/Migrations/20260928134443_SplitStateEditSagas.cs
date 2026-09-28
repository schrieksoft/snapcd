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
    public partial class SplitStateEditSagas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StateMoveSagas");

            migrationBuilder.CreateTable(
                name: "ImportSagas",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    DefinitiveRevision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    InstructionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SucceededJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportSagas", x => new { x.CorrelationId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ImportSagas_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MoveSagas",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    DefinitiveRevision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    InstructionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SucceededJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MoveSagas", x => new { x.CorrelationId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_MoveSagas_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RemoveSagas",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    DefinitiveRevision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    InstructionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SucceededJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoveSagas", x => new { x.CorrelationId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_RemoveSagas_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportSagas_CorrelationId",
                table: "ImportSagas",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportSagas_ModuleId_OrganizationId",
                table: "ImportSagas",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_MoveSagas_CorrelationId",
                table: "MoveSagas",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MoveSagas_ModuleId_OrganizationId",
                table: "MoveSagas",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RemoveSagas_CorrelationId",
                table: "RemoveSagas",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RemoveSagas_ModuleId_OrganizationId",
                table: "RemoveSagas",
                columns: new[] { "ModuleId", "OrganizationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportSagas");

            migrationBuilder.DropTable(
                name: "MoveSagas");

            migrationBuilder.DropTable(
                name: "RemoveSagas");

            migrationBuilder.CreateTable(
                name: "StateMoveSagas",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalTimeoutMinutes = table.Column<int>(type: "int", nullable: true),
                    ApprovalTimeoutScheduleTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DeclaredJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefinitiveRevision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FailedCount = table.Column<int>(type: "int", nullable: false),
                    GracefulCancellationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HeartbeatRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HeartbeatScheduleTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InstructionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    IsDeclined = table.Column<bool>(type: "bit", nullable: false),
                    KillCancellationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreviousStateBeforeCancelling = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PreviousStateBeforeWaiting = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponseAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    RunnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerInstanceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RunnerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ServerInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SucceededJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    WaitingSince = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateMoveSagas", x => new { x.CorrelationId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_StateMoveSagas_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StateMoveSagas_CorrelationId",
                table: "StateMoveSagas",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateMoveSagas_ModuleId_OrganizationId",
                table: "StateMoveSagas",
                columns: new[] { "ModuleId", "OrganizationId" });
        }
    }
}
