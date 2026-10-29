// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnapCd.Server.Host.Database.Migrations
{
    /// <inheritdoc />
    public partial class ProbeResourceChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ResourceChangeSetId",
                table: "ModuleProbeAttempts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ResourceChangeSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProbeWaveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<long>(type: "bigint", nullable: false),
                    TotalCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ResourceChangeSets", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ResourceChangeSets_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceChangeSets_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceChangeSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Actions = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    table.PrimaryKey("PK_ResourceChanges", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ResourceChanges_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceChanges_ResourceChangeSets_ResourceChangeSetId_OrganizationId",
                        columns: x => new { x.ResourceChangeSetId, x.OrganizationId },
                        principalTable: "ResourceChangeSets",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleProbeAttempts_ResourceChangeSetId_OrganizationId",
                table: "ModuleProbeAttempts",
                columns: new[] { "ResourceChangeSetId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceChanges_Id",
                table: "ResourceChanges",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceChanges_OrganizationId",
                table: "ResourceChanges",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceChanges_ResourceChangeSetId_Address_OrganizationId",
                table: "ResourceChanges",
                columns: new[] { "ResourceChangeSetId", "Address", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceChanges_ResourceChangeSetId_OrganizationId",
                table: "ResourceChanges",
                columns: new[] { "ResourceChangeSetId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceChangeSets_Id",
                table: "ResourceChangeSets",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceChangeSets_ModuleId_OrganizationId",
                table: "ResourceChangeSets",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceChangeSets_ModuleId_Timestamp_OrganizationId",
                table: "ResourceChangeSets",
                columns: new[] { "ModuleId", "Timestamp", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceChangeSets_OrganizationId",
                table: "ResourceChangeSets",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceChangeSets_ProbeWaveId",
                table: "ResourceChangeSets",
                column: "ProbeWaveId");

            migrationBuilder.AddForeignKey(
                name: "FK_ModuleProbeAttempts_ResourceChangeSets_ResourceChangeSetId_OrganizationId",
                table: "ModuleProbeAttempts",
                columns: new[] { "ResourceChangeSetId", "OrganizationId" },
                principalTable: "ResourceChangeSets",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModuleProbeAttempts_ResourceChangeSets_ResourceChangeSetId_OrganizationId",
                table: "ModuleProbeAttempts");

            migrationBuilder.DropTable(
                name: "ResourceChanges");

            migrationBuilder.DropTable(
                name: "ResourceChangeSets");

            migrationBuilder.DropIndex(
                name: "IX_ModuleProbeAttempts_ResourceChangeSetId_OrganizationId",
                table: "ModuleProbeAttempts");

            migrationBuilder.DropColumn(
                name: "ResourceChangeSetId",
                table: "ModuleProbeAttempts");
        }
    }
}
