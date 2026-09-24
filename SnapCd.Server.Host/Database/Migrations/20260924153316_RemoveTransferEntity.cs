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
    public partial class RemoveTransferEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransferObjects");

            migrationBuilder.DropTable(
                name: "TransferRuns");

            migrationBuilder.DropTable(
                name: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_TransferMigrateSagas_TransferId_OrganizationId",
                table: "TransferMigrateSagas");

            migrationBuilder.DropIndex(
                name: "IX_TransferMigrateSagas_TransferRunId_ModuleId_OrganizationId",
                table: "TransferMigrateSagas");

            migrationBuilder.DropIndex(
                name: "IX_ManualModuleJobAddresses_JobId_Address_Direction_OrganizationId",
                table: "ManualModuleJobAddresses");

            migrationBuilder.DropColumn(
                name: "TransferId",
                table: "TransferMigrateSagas");

            migrationBuilder.DropColumn(
                name: "TransferRunId",
                table: "ManualModuleJobs");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "ManualModuleJobAddresses");

            migrationBuilder.DropColumn(
                name: "EndedAt",
                table: "ManualModuleJobAddresses");

            // A run id is not a Module id, and TransferRuns is gone in this same migration.
            migrationBuilder.DropColumn(
                name: "TransferRunId",
                table: "TransferMigrateSagas");

            migrationBuilder.AddColumn<Guid>(
                name: "CounterpartyModuleId",
                table: "TransferMigrateSagas",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.RenameColumn(
                name: "StartedAt",
                table: "ManualModuleJobAddresses",
                newName: "RecordedAt");

            migrationBuilder.AddColumn<string>(
                name: "ProveRef",
                table: "ManualModuleJobs",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobAddresses_JobId_Address_Operation_OrganizationId",
                table: "ManualModuleJobAddresses",
                columns: new[] { "JobId", "Address", "Operation", "OrganizationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ManualModuleJobAddresses_JobId_Address_Operation_OrganizationId",
                table: "ManualModuleJobAddresses");

            migrationBuilder.DropColumn(
                name: "ProveRef",
                table: "ManualModuleJobs");

            migrationBuilder.DropColumn(
                name: "CounterpartyModuleId",
                table: "TransferMigrateSagas");

            migrationBuilder.AddColumn<Guid>(
                name: "TransferRunId",
                table: "TransferMigrateSagas",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.RenameColumn(
                name: "RecordedAt",
                table: "ManualModuleJobAddresses",
                newName: "StartedAt");

            migrationBuilder.AddColumn<Guid>(
                name: "TransferId",
                table: "TransferMigrateSagas",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TransferRunId",
                table: "ManualModuleJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "ManualModuleJobAddresses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAt",
                table: "ManualModuleJobAddresses",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Transfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CounterpartyModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CloseReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConsentAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsentDecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConsentPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConsentPrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsentReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConsentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                name: "TransferObjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ArrivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArrivedModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeftAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LeftModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedByJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResolvedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferObjects", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_TransferObjects_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferObjects_Transfers_TransferId_OrganizationId",
                        columns: x => new { x.TransferId, x.OrganizationId },
                        principalTable: "Transfers",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransferRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CounterpartyRef = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ref = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Scope = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferRuns", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_TransferRuns_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferRuns_Transfers_TransferId_OrganizationId",
                        columns: x => new { x.TransferId, x.OrganizationId },
                        principalTable: "Transfers",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransferMigrateSagas_TransferId_OrganizationId",
                table: "TransferMigrateSagas",
                columns: new[] { "TransferId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferMigrateSagas_TransferRunId_ModuleId_OrganizationId",
                table: "TransferMigrateSagas",
                columns: new[] { "TransferRunId", "ModuleId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManualModuleJobAddresses_JobId_Address_Direction_OrganizationId",
                table: "ManualModuleJobAddresses",
                columns: new[] { "JobId", "Address", "Direction", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferObjects_Id",
                table: "TransferObjects",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferObjects_OrganizationId_ArrivedModuleId_ArrivedAt",
                table: "TransferObjects",
                columns: new[] { "OrganizationId", "ArrivedModuleId", "ArrivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferObjects_TransferId_Address_OrganizationId",
                table: "TransferObjects",
                columns: new[] { "TransferId", "Address", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferObjects_TransferId_OrganizationId",
                table: "TransferObjects",
                columns: new[] { "TransferId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferRuns_Id",
                table: "TransferRuns",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferRuns_OrganizationId",
                table: "TransferRuns",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRuns_TransferId_OrganizationId",
                table: "TransferRuns",
                columns: new[] { "TransferId", "OrganizationId" });

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
                name: "IX_Transfers_OrganizationId",
                table: "Transfers",
                column: "OrganizationId");
        }
    }
}
