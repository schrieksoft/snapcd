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
    public partial class AddTriggerPathFiltering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DefaultTriggerPathFilterEnabled",
                table: "Namespaces",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesiredClosureHash",
                table: "ModuleSagas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TriggerPathFilterEnabled",
                table: "Modules",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefinitiveClosureHash",
                table: "ModuleJobs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ModuleAdditionalTriggerPaths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
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
                    table.PrimaryKey("PK_ModuleAdditionalTriggerPaths", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleAdditionalTriggerPaths_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleAdditionalTriggerPaths_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceAdditionalTriggerPaths",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
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
                    table.PrimaryKey("PK_NamespaceAdditionalTriggerPaths", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_NamespaceAdditionalTriggerPaths_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceAdditionalTriggerPaths_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleAdditionalTriggerPaths_Id",
                table: "ModuleAdditionalTriggerPaths",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleAdditionalTriggerPaths_ModuleId_OrganizationId",
                table: "ModuleAdditionalTriggerPaths",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleAdditionalTriggerPaths_ModuleId_Path",
                table: "ModuleAdditionalTriggerPaths",
                columns: new[] { "ModuleId", "Path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleAdditionalTriggerPaths_OrganizationId",
                table: "ModuleAdditionalTriggerPaths",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceAdditionalTriggerPaths_Id",
                table: "NamespaceAdditionalTriggerPaths",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceAdditionalTriggerPaths_NamespaceId_OrganizationId",
                table: "NamespaceAdditionalTriggerPaths",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceAdditionalTriggerPaths_NamespaceId_Path",
                table: "NamespaceAdditionalTriggerPaths",
                columns: new[] { "NamespaceId", "Path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceAdditionalTriggerPaths_OrganizationId",
                table: "NamespaceAdditionalTriggerPaths",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModuleAdditionalTriggerPaths");

            migrationBuilder.DropTable(
                name: "NamespaceAdditionalTriggerPaths");

            migrationBuilder.DropColumn(
                name: "DefaultTriggerPathFilterEnabled",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DesiredClosureHash",
                table: "ModuleSagas");

            migrationBuilder.DropColumn(
                name: "TriggerPathFilterEnabled",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "DefinitiveClosureHash",
                table: "ModuleJobs");
        }
    }
}
