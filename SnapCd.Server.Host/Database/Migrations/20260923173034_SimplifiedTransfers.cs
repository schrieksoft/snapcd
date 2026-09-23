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
    public partial class SimplifiedTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransferParticipantSagas");

            migrationBuilder.DropTable(
                name: "TransferSagas");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_MapHash",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "CloseReason",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "MapHash",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "MapJson",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "MapRef",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "PrepareError",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ReceiverLockedAt",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ReceiverLockedBy",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ReceiverLockedByPrincipalDiscriminator",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ReceiverMergedCommit",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ReceiverMergedDeclaredAt",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ReceiverMergedDeclaredBy",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ReceiverMergedDeclaredByPrincipalDiscriminator",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ReceiverReleasedAt",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "SourceLockedAt",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "SourceLockedBy",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "SourceLockedByPrincipalDiscriminator",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "SourceMergedCommit",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "SourceMergedDeclaredAt",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "SourceMergedDeclaredBy",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "SourceMergedDeclaredByPrincipalDiscriminator",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "SourceReleasedAt",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "HeldAt",
                table: "ModuleSagas");

            migrationBuilder.DropColumn(
                name: "HeldByTransferId",
                table: "ModuleSagas");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Transfers",
                newName: "Scope");

            migrationBuilder.AddColumn<Guid>(
                name: "TransferId",
                table: "ManualModuleJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TransferMigrateSagas",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RootDirectory = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProveRef = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DefinitiveRevision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FragmentState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FragmentMeta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProducedFragmentState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProducedFragmentMeta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProveExitCode = table.Column<int>(type: "int", nullable: true),
                    Verdict = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    ServerInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferMigrateSagas", x => new { x.CorrelationId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_TransferMigrateSagas_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransferMigrateSagas_CorrelationId",
                table: "TransferMigrateSagas",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferMigrateSagas_ModuleId_OrganizationId",
                table: "TransferMigrateSagas",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferMigrateSagas_TransferId_Role_OrganizationId",
                table: "TransferMigrateSagas",
                columns: new[] { "TransferId", "Role", "OrganizationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransferMigrateSagas");

            migrationBuilder.DropColumn(
                name: "TransferId",
                table: "ManualModuleJobs");

            migrationBuilder.RenameColumn(
                name: "Scope",
                table: "Transfers",
                newName: "Status");

            migrationBuilder.AddColumn<string>(
                name: "CloseReason",
                table: "Transfers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ClosedAt",
                table: "Transfers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MapHash",
                table: "Transfers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MapJson",
                table: "Transfers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MapRef",
                table: "Transfers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrepareError",
                table: "Transfers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReceiverLockedAt",
                table: "Transfers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceiverLockedBy",
                table: "Transfers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverLockedByPrincipalDiscriminator",
                table: "Transfers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverMergedCommit",
                table: "Transfers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReceiverMergedDeclaredAt",
                table: "Transfers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceiverMergedDeclaredBy",
                table: "Transfers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverMergedDeclaredByPrincipalDiscriminator",
                table: "Transfers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReceiverReleasedAt",
                table: "Transfers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourceLockedAt",
                table: "Transfers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceLockedBy",
                table: "Transfers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceLockedByPrincipalDiscriminator",
                table: "Transfers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceMergedCommit",
                table: "Transfers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourceMergedDeclaredAt",
                table: "Transfers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceMergedDeclaredBy",
                table: "Transfers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceMergedDeclaredByPrincipalDiscriminator",
                table: "Transfers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SourceReleasedAt",
                table: "Transfers",
                type: "datetimeoffset",
                nullable: true);

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
                name: "TransferParticipantSagas",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalTimeoutMinutes = table.Column<int>(type: "int", nullable: true),
                    ApprovalTimeoutScheduleTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DeclaredJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefinitiveRevision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FragmentMeta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FragmentState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GracefulCancellationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HeartbeatRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HeartbeatScheduleTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    InputKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    IsDeclined = table.Column<bool>(type: "bit", nullable: false),
                    KillCancellationRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastProvenFragmentMeta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastProvenFragmentState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastProvenKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastProvenOutputsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Map = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NeedsValuesFromJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OutputsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviousStateBeforeCancelling = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PreviousStateBeforeWaiting = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ProducedFragmentMeta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProducedFragmentState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProveExitCode = table.Column<int>(type: "int", nullable: true),
                    ProveRef = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ProveRound = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponseAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RootDirectory = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    RunnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerInstanceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RunnerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ServerInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StopAfterMap = table.Column<bool>(type: "bit", nullable: false),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Verdict = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    WaitingSince = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferParticipantSagas", x => new { x.CorrelationId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_TransferParticipantSagas_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransferSagas",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentState = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Map = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MapHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ProveRound = table.Column<int>(type: "int", nullable: false),
                    ReceiverModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiverProveRef = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ReceiverRan = table.Column<bool>(type: "bit", nullable: false),
                    ReceiverSagaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    SourceModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceNeedsValues = table.Column<bool>(type: "bit", nullable: false),
                    SourceProveRef = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SourceRan = table.Column<bool>(type: "bit", nullable: false),
                    SourceSagaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StallReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferSagas", x => new { x.CorrelationId, x.OrganizationId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_MapHash",
                table: "Transfers",
                column: "MapHash");

            migrationBuilder.CreateIndex(
                name: "IX_TransferParticipantSagas_CorrelationId",
                table: "TransferParticipantSagas",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferParticipantSagas_ModuleId_OrganizationId",
                table: "TransferParticipantSagas",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferParticipantSagas_TransferId_Role_OrganizationId",
                table: "TransferParticipantSagas",
                columns: new[] { "TransferId", "Role", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferSagas_CorrelationId",
                table: "TransferSagas",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferSagas_TransferId_OrganizationId",
                table: "TransferSagas",
                columns: new[] { "TransferId", "OrganizationId" },
                unique: true);
        }
    }
}
