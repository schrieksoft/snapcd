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
    public partial class AddProbeWavesAndSignalOutputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SelectedRunnerId",
                table: "StateMigrationJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SelectedRunnerId",
                table: "ModuleJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProbeWaves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaxDepth = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ProbeWaves", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ProbeWaves_Modules_OriginModuleId_OrganizationId",
                        columns: x => new { x.OriginModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProbeWaves_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SignalOutputSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProbeWaveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<long>(type: "bigint", nullable: false),
                    Checksum = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
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
                    table.PrimaryKey("PK_SignalOutputSets", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_SignalOutputSets_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SignalOutputSets_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProbeWaveParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProbeWaveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignalOutputSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastProbeModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_ProbeWaveParticipants", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ProbeWaveParticipants_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProbeWaveParticipants_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProbeWaveParticipants_ProbeWaves_ProbeWaveId_OrganizationId",
                        columns: x => new { x.ProbeWaveId, x.OrganizationId },
                        principalTable: "ProbeWaves",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProbeWaveParticipants_SignalOutputSets_SignalOutputSetId_OrganizationId",
                        columns: x => new { x.SignalOutputSetId, x.OrganizationId },
                        principalTable: "SignalOutputSets",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SignalOutputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignalOutputSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Sensitive = table.Column<bool>(type: "bit", nullable: false),
                    Discriminator = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    SignalJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemoteSecretName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
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
                    table.PrimaryKey("PK_SignalOutputs", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_SignalOutputs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SignalOutputs_SignalOutputSets_SignalOutputSetId_OrganizationId",
                        columns: x => new { x.SignalOutputSetId, x.OrganizationId },
                        principalTable: "SignalOutputSets",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SignalContributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProbeWaveParticipantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpstreamParticipantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_SignalContributions", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_SignalContributions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SignalContributions_ProbeWaveParticipants_ProbeWaveParticipantId_OrganizationId",
                        columns: x => new { x.ProbeWaveParticipantId, x.OrganizationId },
                        principalTable: "ProbeWaveParticipants",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SignalContributions_ProbeWaveParticipants_UpstreamParticipantId_OrganizationId",
                        columns: x => new { x.UpstreamParticipantId, x.OrganizationId },
                        principalTable: "ProbeWaveParticipants",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProbeWaveParticipants_Id",
                table: "ProbeWaveParticipants",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProbeWaveParticipants_ModuleId_OrganizationId",
                table: "ProbeWaveParticipants",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProbeWaveParticipants_OrganizationId",
                table: "ProbeWaveParticipants",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProbeWaveParticipants_ProbeWaveId_ModuleId_OrganizationId",
                table: "ProbeWaveParticipants",
                columns: new[] { "ProbeWaveId", "ModuleId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProbeWaveParticipants_ProbeWaveId_OrganizationId",
                table: "ProbeWaveParticipants",
                columns: new[] { "ProbeWaveId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProbeWaveParticipants_SignalOutputSetId_OrganizationId",
                table: "ProbeWaveParticipants",
                columns: new[] { "SignalOutputSetId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProbeWaves_Id",
                table: "ProbeWaves",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProbeWaves_OrganizationId",
                table: "ProbeWaves",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProbeWaves_OriginModuleId_OrganizationId",
                table: "ProbeWaves",
                columns: new[] { "OriginModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProbeWaves_OriginModuleJobId_OrganizationId",
                table: "ProbeWaves",
                columns: new[] { "OriginModuleJobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalContributions_Id",
                table: "SignalContributions",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignalContributions_OrganizationId",
                table: "SignalContributions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SignalContributions_ProbeWaveParticipantId_OrganizationId",
                table: "SignalContributions",
                columns: new[] { "ProbeWaveParticipantId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalContributions_ProbeWaveParticipantId_Status_OrganizationId",
                table: "SignalContributions",
                columns: new[] { "ProbeWaveParticipantId", "Status", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalContributions_ProbeWaveParticipantId_UpstreamParticipantId_OrganizationId",
                table: "SignalContributions",
                columns: new[] { "ProbeWaveParticipantId", "UpstreamParticipantId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignalContributions_UpstreamParticipantId_OrganizationId",
                table: "SignalContributions",
                columns: new[] { "UpstreamParticipantId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutputs_Id",
                table: "SignalOutputs",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutputs_OrganizationId",
                table: "SignalOutputs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutputs_SignalOutputSetId_Name_OrganizationId",
                table: "SignalOutputs",
                columns: new[] { "SignalOutputSetId", "Name", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutputs_SignalOutputSetId_OrganizationId",
                table: "SignalOutputs",
                columns: new[] { "SignalOutputSetId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutputSets_Id",
                table: "SignalOutputSets",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutputSets_ModuleId_OrganizationId",
                table: "SignalOutputSets",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutputSets_ModuleId_Timestamp_OrganizationId",
                table: "SignalOutputSets",
                columns: new[] { "ModuleId", "Timestamp", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutputSets_OrganizationId",
                table: "SignalOutputSets",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutputSets_ProbeWaveId",
                table: "SignalOutputSets",
                column: "ProbeWaveId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignalContributions");

            migrationBuilder.DropTable(
                name: "SignalOutputs");

            migrationBuilder.DropTable(
                name: "ProbeWaveParticipants");

            migrationBuilder.DropTable(
                name: "ProbeWaves");

            migrationBuilder.DropTable(
                name: "SignalOutputSets");

            migrationBuilder.DropColumn(
                name: "SelectedRunnerId",
                table: "StateMigrationJobs");

            migrationBuilder.DropColumn(
                name: "SelectedRunnerId",
                table: "ModuleJobs");
        }
    }
}
