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
    public partial class ReshapeProbeWaveEdges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignalContributions");

            migrationBuilder.DropTable(
                name: "ProbeWaveParticipants");

            migrationBuilder.CreateTable(
                name: "ModuleProbeAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProbeWaveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignalOutputSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProbeModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_ModuleProbeAttempts", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleProbeAttempts_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModuleProbeAttempts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModuleProbeAttempts_ProbeWaves_ProbeWaveId_OrganizationId",
                        columns: x => new { x.ProbeWaveId, x.OrganizationId },
                        principalTable: "ProbeWaves",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleProbeAttempts_SignalOutputSets_SignalOutputSetId_OrganizationId",
                        columns: x => new { x.SignalOutputSetId, x.OrganizationId },
                        principalTable: "SignalOutputSets",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SignalEdges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProbeWaveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsumerModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpstreamModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_SignalEdges", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_SignalEdges_Modules_ConsumerModuleId_OrganizationId",
                        columns: x => new { x.ConsumerModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SignalEdges_Modules_UpstreamModuleId_OrganizationId",
                        columns: x => new { x.UpstreamModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SignalEdges_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SignalEdges_ProbeWaves_ProbeWaveId_OrganizationId",
                        columns: x => new { x.ProbeWaveId, x.OrganizationId },
                        principalTable: "ProbeWaves",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleProbeAttempts_Id",
                table: "ModuleProbeAttempts",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleProbeAttempts_ModuleId_OrganizationId",
                table: "ModuleProbeAttempts",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleProbeAttempts_OrganizationId",
                table: "ModuleProbeAttempts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleProbeAttempts_ProbeWaveId_ModuleId_OrganizationId",
                table: "ModuleProbeAttempts",
                columns: new[] { "ProbeWaveId", "ModuleId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleProbeAttempts_ProbeWaveId_OrganizationId",
                table: "ModuleProbeAttempts",
                columns: new[] { "ProbeWaveId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleProbeAttempts_SignalOutputSetId_OrganizationId",
                table: "ModuleProbeAttempts",
                columns: new[] { "SignalOutputSetId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalEdges_ConsumerModuleId_OrganizationId",
                table: "SignalEdges",
                columns: new[] { "ConsumerModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalEdges_Id",
                table: "SignalEdges",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignalEdges_OrganizationId",
                table: "SignalEdges",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SignalEdges_ProbeWaveId_ConsumerModuleId_Status_OrganizationId",
                table: "SignalEdges",
                columns: new[] { "ProbeWaveId", "ConsumerModuleId", "Status", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalEdges_ProbeWaveId_ConsumerModuleId_UpstreamModuleId_OrganizationId",
                table: "SignalEdges",
                columns: new[] { "ProbeWaveId", "ConsumerModuleId", "UpstreamModuleId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SignalEdges_ProbeWaveId_OrganizationId",
                table: "SignalEdges",
                columns: new[] { "ProbeWaveId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalEdges_UpstreamModuleId_OrganizationId",
                table: "SignalEdges",
                columns: new[] { "UpstreamModuleId", "OrganizationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModuleProbeAttempts");

            migrationBuilder.DropTable(
                name: "SignalEdges");

            migrationBuilder.CreateTable(
                name: "ProbeWaveParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProbeWaveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignalOutputSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastProbeModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                name: "SignalContributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProbeWaveParticipantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpstreamParticipantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
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
        }
    }
}
