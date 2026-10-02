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
    public partial class StateMigrationsAndTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GracefulCancellationRequestId",
                table: "DestroyJobSagas");

            migrationBuilder.DropColumn(
                name: "GracefulCancellationRequestId",
                table: "ApplyJobSagas");

            migrationBuilder.AddColumn<int>(
                name: "DefaultStateMigrationApprovalThreshold",
                table: "Namespaces",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PauseReason",
                table: "ModuleSagas",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Paused",
                table: "ModuleSagas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PausedAt",
                table: "ModuleSagas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PausedBy",
                table: "ModuleSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StateMigrationApprovalThreshold",
                table: "Modules",
                type: "int",
                nullable: true);

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
                name: "LookupAddressesSagas",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddressesJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
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
                    table.PrimaryKey("PK_LookupAddressesSagas", x => new { x.CorrelationId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_LookupAddressesSagas_Modules_ModuleId_OrganizationId",
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

            migrationBuilder.CreateTable(
                name: "SplitMigrateSagas",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RootDirectory = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Force = table.Column<bool>(type: "bit", nullable: false),
                    RederiveBackend = table.Column<bool>(type: "bit", nullable: false),
                    StopAfterProve = table.Column<bool>(type: "bit", nullable: false),
                    RefactorMapHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CarvedModuleNames = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ProvenModuleCount = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_SplitMigrateSagas", x => new { x.CorrelationId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_SplitMigrateSagas_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateMigrationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProveRef = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    JobNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TimestampStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TimestampEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JobType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WaitingForApproval = table.Column<bool>(type: "bit", nullable: true),
                    WaitingForRunner = table.Column<bool>(type: "bit", nullable: true),
                    WaitingForConsent = table.Column<bool>(type: "bit", nullable: true),
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
                    table.PrimaryKey("PK_StateMigrationJobs", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_StateMigrationJobs_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StateMigrationJobs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferMigrateSagas",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CounterpartyModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RootDirectory = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProveRef = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DefinitiveRevision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    NeedsOutputsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsSource = table.Column<bool>(type: "bit", nullable: false),
                    ProveExitCode = table.Column<int>(type: "int", nullable: true),
                    Verdict = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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

            migrationBuilder.CreateTable(
                name: "Transfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CounterpartyModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConsentDecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConsentPrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsentPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ModuleRef = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CounterpartyRef = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CloseReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                        name: "FK_Transfers_Modules_CounterpartyModuleId_OrganizationId",
                        columns: x => new { x.CounterpartyModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transfers_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
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

            migrationBuilder.CreateTable(
                name: "RunnerConnectionStateMigrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunnerConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StateMigrationJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_RunnerConnectionStateMigrations", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_RunnerConnectionStateMigrations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RunnerConnectionStateMigrations_RunnerConnections_RunnerConnectionId_OrganizationId",
                        columns: x => new { x.RunnerConnectionId, x.OrganizationId },
                        principalTable: "RunnerConnections",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RunnerConnectionStateMigrations_StateMigrationJobs_StateMigrationJobId_OrganizationId",
                        columns: x => new { x.StateMigrationJobId, x.OrganizationId },
                        principalTable: "StateMigrationJobs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateMigrationJobAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Target = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
                    table.PrimaryKey("PK_StateMigrationJobAddresses", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_StateMigrationJobAddresses_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateMigrationJobAddresses_StateMigrationJobs_JobId_OrganizationId",
                        columns: x => new { x.JobId, x.OrganizationId },
                        principalTable: "StateMigrationJobs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateMigrationJobApprovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StateMigrationJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_StateMigrationJobApprovals", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_StateMigrationJobApprovals_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateMigrationJobApprovals_StateMigrationJobs_StateMigrationJobId_OrganizationId",
                        columns: x => new { x.StateMigrationJobId, x.OrganizationId },
                        principalTable: "StateMigrationJobs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateMigrationJobArtefacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Ciphertext = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_StateMigrationJobArtefacts", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_StateMigrationJobArtefacts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateMigrationJobArtefacts_StateMigrationJobs_JobId_OrganizationId",
                        columns: x => new { x.JobId, x.OrganizationId },
                        principalTable: "StateMigrationJobs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateMigrationJobSteps",
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
                    table.PrimaryKey("PK_StateMigrationJobSteps", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_StateMigrationJobSteps_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateMigrationJobSteps_StateMigrationJobs_JobId_OrganizationId",
                        columns: x => new { x.JobId, x.OrganizationId },
                        principalTable: "StateMigrationJobs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransferArtefacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceFragmentCiphertext = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceFragmentMetaCiphertext = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiverOutputsCiphertext = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_TransferArtefacts", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_TransferArtefacts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferArtefacts_Transfers_TransferId_OrganizationId",
                        columns: x => new { x.TransferId, x.OrganizationId },
                        principalTable: "Transfers",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransferLocks",
                columns: table => new
                {
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferLocks", x => new { x.OrganizationId, x.ModuleId });
                    table.ForeignKey(
                        name: "FK_TransferLocks_Transfers_TransferId_OrganizationId",
                        columns: x => new { x.TransferId, x.OrganizationId },
                        principalTable: "Transfers",
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
                name: "IX_LookupAddressesSagas_CorrelationId",
                table: "LookupAddressesSagas",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupAddressesSagas_ModuleId_OrganizationId",
                table: "LookupAddressesSagas",
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

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionStateMigrations_Id",
                table: "RunnerConnectionStateMigrations",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionStateMigrations_OrganizationId",
                table: "RunnerConnectionStateMigrations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionStateMigrations_RunnerConnectionId_OrganizationId",
                table: "RunnerConnectionStateMigrations",
                columns: new[] { "RunnerConnectionId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionStateMigrations_RunnerConnectionId_StateMigrationJobId_OrganizationId",
                table: "RunnerConnectionStateMigrations",
                columns: new[] { "RunnerConnectionId", "StateMigrationJobId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunnerConnectionStateMigrations_StateMigrationJobId_OrganizationId",
                table: "RunnerConnectionStateMigrations",
                columns: new[] { "StateMigrationJobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SplitMigrateSagas_CorrelationId",
                table: "SplitMigrateSagas",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SplitMigrateSagas_ModuleId_OrganizationId",
                table: "SplitMigrateSagas",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobAddresses_Id",
                table: "StateMigrationJobAddresses",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobAddresses_JobId_Address_Operation_OrganizationId",
                table: "StateMigrationJobAddresses",
                columns: new[] { "JobId", "Address", "Operation", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobAddresses_JobId_OrganizationId",
                table: "StateMigrationJobAddresses",
                columns: new[] { "JobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobAddresses_OrganizationId_ModuleId_Address",
                table: "StateMigrationJobAddresses",
                columns: new[] { "OrganizationId", "ModuleId", "Address" });

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobApprovals_Id",
                table: "StateMigrationJobApprovals",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobApprovals_OrganizationId",
                table: "StateMigrationJobApprovals",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobApprovals_PrincipalId",
                table: "StateMigrationJobApprovals",
                column: "PrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobApprovals_StateMigrationJobId",
                table: "StateMigrationJobApprovals",
                column: "StateMigrationJobId");

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobApprovals_StateMigrationJobId_OrganizationId",
                table: "StateMigrationJobApprovals",
                columns: new[] { "StateMigrationJobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobApprovals_StateMigrationJobId_PrincipalId_OrganizationId",
                table: "StateMigrationJobApprovals",
                columns: new[] { "StateMigrationJobId", "PrincipalId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobArtefacts_Id",
                table: "StateMigrationJobArtefacts",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobArtefacts_JobId_Name_OrganizationId",
                table: "StateMigrationJobArtefacts",
                columns: new[] { "JobId", "Name", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobArtefacts_JobId_OrganizationId",
                table: "StateMigrationJobArtefacts",
                columns: new[] { "JobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobArtefacts_OrganizationId",
                table: "StateMigrationJobArtefacts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobs_Id",
                table: "StateMigrationJobs",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobs_ModuleId",
                table: "StateMigrationJobs",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobs_ModuleId_OrganizationId",
                table: "StateMigrationJobs",
                columns: new[] { "ModuleId", "OrganizationId" },
                unique: true,
                filter: "[Status] = 'Running'");

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobs_ModuleId_TimestampStart_OrganizationId",
                table: "StateMigrationJobs",
                columns: new[] { "ModuleId", "TimestampStart", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobs_OrganizationId",
                table: "StateMigrationJobs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobSteps_Id",
                table: "StateMigrationJobSteps",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobSteps_JobId_ModuleId_Task_Attempt_OrganizationId",
                table: "StateMigrationJobSteps",
                columns: new[] { "JobId", "ModuleId", "Task", "Attempt", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobSteps_JobId_OrganizationId",
                table: "StateMigrationJobSteps",
                columns: new[] { "JobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobSteps_ModuleId",
                table: "StateMigrationJobSteps",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobSteps_OrganizationId",
                table: "StateMigrationJobSteps",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobSteps_TransferId",
                table: "StateMigrationJobSteps",
                column: "TransferId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferArtefacts_Id",
                table: "TransferArtefacts",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferArtefacts_OrganizationId",
                table: "TransferArtefacts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferArtefacts_TransferId_OrganizationId",
                table: "TransferArtefacts",
                columns: new[] { "TransferId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferLocks_TransferId_OrganizationId",
                table: "TransferLocks",
                columns: new[] { "TransferId", "OrganizationId" });

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
                name: "IX_Transfers_CounterpartyModuleId_OrganizationId",
                table: "Transfers",
                columns: new[] { "CounterpartyModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_Id",
                table: "Transfers",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_ModuleId_OrganizationId",
                table: "Transfers",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_OrganizationId_CounterpartyModuleId_ClosedAt",
                table: "Transfers",
                columns: new[] { "OrganizationId", "CounterpartyModuleId", "ClosedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_OrganizationId_ModuleId_ClosedAt",
                table: "Transfers",
                columns: new[] { "OrganizationId", "ModuleId", "ClosedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportSagas");

            migrationBuilder.DropTable(
                name: "LookupAddressesSagas");

            migrationBuilder.DropTable(
                name: "MoveSagas");

            migrationBuilder.DropTable(
                name: "RemoveSagas");

            migrationBuilder.DropTable(
                name: "RunnerConnectionStateMigrations");

            migrationBuilder.DropTable(
                name: "SplitMigrateSagas");

            migrationBuilder.DropTable(
                name: "StateMigrationJobAddresses");

            migrationBuilder.DropTable(
                name: "StateMigrationJobApprovals");

            migrationBuilder.DropTable(
                name: "StateMigrationJobArtefacts");

            migrationBuilder.DropTable(
                name: "StateMigrationJobSteps");

            migrationBuilder.DropTable(
                name: "TransferArtefacts");

            migrationBuilder.DropTable(
                name: "TransferLocks");

            migrationBuilder.DropTable(
                name: "TransferMigrateSagas");

            migrationBuilder.DropTable(
                name: "StateMigrationJobs");

            migrationBuilder.DropTable(
                name: "Transfers");

            migrationBuilder.DropColumn(
                name: "DefaultStateMigrationApprovalThreshold",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "PauseReason",
                table: "ModuleSagas");

            migrationBuilder.DropColumn(
                name: "Paused",
                table: "ModuleSagas");

            migrationBuilder.DropColumn(
                name: "PausedAt",
                table: "ModuleSagas");

            migrationBuilder.DropColumn(
                name: "PausedBy",
                table: "ModuleSagas");

            migrationBuilder.DropColumn(
                name: "StateMigrationApprovalThreshold",
                table: "Modules");

            migrationBuilder.AddColumn<Guid>(
                name: "GracefulCancellationRequestId",
                table: "DestroyJobSagas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GracefulCancellationRequestId",
                table: "ApplyJobSagas",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
