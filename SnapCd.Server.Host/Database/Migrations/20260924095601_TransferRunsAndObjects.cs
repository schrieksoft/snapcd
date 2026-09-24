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
    public partial class TransferRunsAndObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transfers_Modules_ReceiverModuleId_OrganizationId",
                table: "Transfers");

            migrationBuilder.DropForeignKey(
                name: "FK_Transfers_Modules_SourceModuleId_OrganizationId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_ReceiverModuleId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_Transfers_SourceModuleId",
                table: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_TransferMigrateSagas_TransferId_Role_OrganizationId",
                table: "TransferMigrateSagas");

            migrationBuilder.DropColumn(
                name: "ReceiverConsentStatus",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ReceiverProveRef",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "SourceProveRef",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "FragmentMeta",
                table: "TransferMigrateSagas");

            migrationBuilder.DropColumn(
                name: "FragmentState",
                table: "TransferMigrateSagas");

            migrationBuilder.DropColumn(
                name: "OutputsJson",
                table: "TransferMigrateSagas");

            migrationBuilder.DropColumn(
                name: "ProducedFragmentMeta",
                table: "TransferMigrateSagas");

            migrationBuilder.DropColumn(
                name: "ProducedFragmentState",
                table: "TransferMigrateSagas");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "TransferMigrateSagas");

            migrationBuilder.RenameColumn(
                name: "SourceModuleId",
                table: "Transfers",
                newName: "ModuleId");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Transfers");

            migrationBuilder.AddColumn<string>(
                name: "ConsentStatus",
                table: "Transfers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.RenameColumn(
                name: "ReceiverModuleId",
                table: "Transfers",
                newName: "CounterpartyModuleId");

            migrationBuilder.RenameColumn(
                name: "ReceiverConsentReason",
                table: "Transfers",
                newName: "ConsentReason");

            migrationBuilder.RenameColumn(
                name: "ReceiverConsentPrincipalId",
                table: "Transfers",
                newName: "ConsentPrincipalId");

            migrationBuilder.RenameColumn(
                name: "ReceiverConsentPrincipalDiscriminator",
                table: "Transfers",
                newName: "ConsentPrincipalDiscriminator");

            migrationBuilder.RenameColumn(
                name: "ReceiverConsentDecidedAt",
                table: "Transfers",
                newName: "ConsentDecidedAt");

            migrationBuilder.RenameColumn(
                name: "ReceiverConsentAgentId",
                table: "Transfers",
                newName: "ConsentAgentId");

            migrationBuilder.RenameIndex(
                name: "IX_Transfers_SourceModuleId_OrganizationId",
                table: "Transfers",
                newName: "IX_Transfers_ModuleId_OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Transfers_ReceiverModuleId_OrganizationId",
                table: "Transfers",
                newName: "IX_Transfers_CounterpartyModuleId_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TransferId",
                table: "ManualModuleJobs",
                newName: "TransferRunId");

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

            migrationBuilder.AddColumn<Guid>(
                name: "ClosedBy",
                table: "Transfers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedByPrincipalDiscriminator",
                table: "Transfers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransferRunId",
                table: "TransferMigrateSagas",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "TransferObjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LeftModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeftAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArrivedModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArrivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedByJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResolvedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    Scope = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ref = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CounterpartyRef = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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

            migrationBuilder.AddForeignKey(
                name: "FK_Transfers_Modules_CounterpartyModuleId_OrganizationId",
                table: "Transfers",
                columns: new[] { "CounterpartyModuleId", "OrganizationId" },
                principalTable: "Modules",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transfers_Modules_ModuleId_OrganizationId",
                table: "Transfers",
                columns: new[] { "ModuleId", "OrganizationId" },
                principalTable: "Modules",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transfers_Modules_CounterpartyModuleId_OrganizationId",
                table: "Transfers");

            migrationBuilder.DropForeignKey(
                name: "FK_Transfers_Modules_ModuleId_OrganizationId",
                table: "Transfers");

            migrationBuilder.DropTable(
                name: "TransferObjects");

            migrationBuilder.DropTable(
                name: "TransferRuns");

            migrationBuilder.DropIndex(
                name: "IX_TransferMigrateSagas_TransferId_OrganizationId",
                table: "TransferMigrateSagas");

            migrationBuilder.DropIndex(
                name: "IX_TransferMigrateSagas_TransferRunId_ModuleId_OrganizationId",
                table: "TransferMigrateSagas");

            migrationBuilder.DropColumn(
                name: "CloseReason",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ClosedBy",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ClosedByPrincipalDiscriminator",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "TransferRunId",
                table: "TransferMigrateSagas");

            migrationBuilder.RenameColumn(
                name: "ModuleId",
                table: "Transfers",
                newName: "SourceModuleId");

            migrationBuilder.RenameColumn(
                name: "CounterpartyModuleId",
                table: "Transfers",
                newName: "ReceiverModuleId");

            migrationBuilder.DropColumn(
                name: "ConsentStatus",
                table: "Transfers");

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "Transfers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Both");

            migrationBuilder.RenameColumn(
                name: "ConsentReason",
                table: "Transfers",
                newName: "ReceiverConsentReason");

            migrationBuilder.RenameColumn(
                name: "ConsentPrincipalId",
                table: "Transfers",
                newName: "ReceiverConsentPrincipalId");

            migrationBuilder.RenameColumn(
                name: "ConsentPrincipalDiscriminator",
                table: "Transfers",
                newName: "ReceiverConsentPrincipalDiscriminator");

            migrationBuilder.RenameColumn(
                name: "ConsentDecidedAt",
                table: "Transfers",
                newName: "ReceiverConsentDecidedAt");

            migrationBuilder.RenameColumn(
                name: "ConsentAgentId",
                table: "Transfers",
                newName: "ReceiverConsentAgentId");

            migrationBuilder.RenameIndex(
                name: "IX_Transfers_ModuleId_OrganizationId",
                table: "Transfers",
                newName: "IX_Transfers_SourceModuleId_OrganizationId");

            migrationBuilder.RenameIndex(
                name: "IX_Transfers_CounterpartyModuleId_OrganizationId",
                table: "Transfers",
                newName: "IX_Transfers_ReceiverModuleId_OrganizationId");

            migrationBuilder.RenameColumn(
                name: "TransferRunId",
                table: "ManualModuleJobs",
                newName: "TransferId");

            migrationBuilder.AddColumn<string>(
                name: "ReceiverConsentStatus",
                table: "Transfers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReceiverProveRef",
                table: "Transfers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceProveRef",
                table: "Transfers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FragmentMeta",
                table: "TransferMigrateSagas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FragmentState",
                table: "TransferMigrateSagas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutputsJson",
                table: "TransferMigrateSagas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProducedFragmentMeta",
                table: "TransferMigrateSagas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProducedFragmentState",
                table: "TransferMigrateSagas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "TransferMigrateSagas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_ReceiverModuleId",
                table: "Transfers",
                column: "ReceiverModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_SourceModuleId",
                table: "Transfers",
                column: "SourceModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferMigrateSagas_TransferId_Role_OrganizationId",
                table: "TransferMigrateSagas",
                columns: new[] { "TransferId", "Role", "OrganizationId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transfers_Modules_ReceiverModuleId_OrganizationId",
                table: "Transfers",
                columns: new[] { "ReceiverModuleId", "OrganizationId" },
                principalTable: "Modules",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transfers_Modules_SourceModuleId_OrganizationId",
                table: "Transfers",
                columns: new[] { "SourceModuleId", "OrganizationId" },
                principalTable: "Modules",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
