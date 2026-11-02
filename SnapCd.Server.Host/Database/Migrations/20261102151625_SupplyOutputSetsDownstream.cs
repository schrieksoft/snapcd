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
    public partial class SupplyOutputSetsDownstream : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutputSetModuleSupplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsumingModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_OutputSetModuleSupplies", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_OutputSetModuleSupplies_Modules_ConsumingModuleId_OrganizationId",
                        columns: x => new { x.ConsumingModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OutputSetModuleSupplies_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutputSetModuleSupplies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutputSetNamespaceSupplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsumingNamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_OutputSetNamespaceSupplies", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_OutputSetNamespaceSupplies_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutputSetNamespaceSupplies_Namespaces_ConsumingNamespaceId_OrganizationId",
                        columns: x => new { x.ConsumingNamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OutputSetNamespaceSupplies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutputSetModuleSupplies_ConsumingModuleId",
                table: "OutputSetModuleSupplies",
                column: "ConsumingModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_OutputSetModuleSupplies_ConsumingModuleId_OrganizationId",
                table: "OutputSetModuleSupplies",
                columns: new[] { "ConsumingModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutputSetModuleSupplies_Id",
                table: "OutputSetModuleSupplies",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutputSetModuleSupplies_ModuleId",
                table: "OutputSetModuleSupplies",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_OutputSetModuleSupplies_ModuleId_ConsumingModuleId_OrganizationId",
                table: "OutputSetModuleSupplies",
                columns: new[] { "ModuleId", "ConsumingModuleId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutputSetModuleSupplies_ModuleId_OrganizationId",
                table: "OutputSetModuleSupplies",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutputSetModuleSupplies_OrganizationId",
                table: "OutputSetModuleSupplies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutputSetNamespaceSupplies_ConsumingNamespaceId",
                table: "OutputSetNamespaceSupplies",
                column: "ConsumingNamespaceId");

            migrationBuilder.CreateIndex(
                name: "IX_OutputSetNamespaceSupplies_ConsumingNamespaceId_OrganizationId",
                table: "OutputSetNamespaceSupplies",
                columns: new[] { "ConsumingNamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutputSetNamespaceSupplies_Id",
                table: "OutputSetNamespaceSupplies",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutputSetNamespaceSupplies_ModuleId",
                table: "OutputSetNamespaceSupplies",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_OutputSetNamespaceSupplies_ModuleId_ConsumingNamespaceId_OrganizationId",
                table: "OutputSetNamespaceSupplies",
                columns: new[] { "ModuleId", "ConsumingNamespaceId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutputSetNamespaceSupplies_ModuleId_OrganizationId",
                table: "OutputSetNamespaceSupplies",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutputSetNamespaceSupplies_OrganizationId",
                table: "OutputSetNamespaceSupplies",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutputSetModuleSupplies");

            migrationBuilder.DropTable(
                name: "OutputSetNamespaceSupplies");
        }
    }
}
