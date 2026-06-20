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
    public partial class AddIntegrationSupply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAssignedToAllModules",
                table: "Integrations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "IntegrationModuleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_IntegrationModuleAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_IntegrationModuleAssignments_Integrations_IntegrationId_OrganizationId",
                        columns: x => new { x.IntegrationId, x.OrganizationId },
                        principalTable: "Integrations",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntegrationModuleAssignments_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntegrationModuleAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationNamespaceAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_IntegrationNamespaceAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_IntegrationNamespaceAssignments_Integrations_IntegrationId_OrganizationId",
                        columns: x => new { x.IntegrationId, x.OrganizationId },
                        principalTable: "Integrations",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntegrationNamespaceAssignments_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntegrationNamespaceAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationStackAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_IntegrationStackAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_IntegrationStackAssignments_Integrations_IntegrationId_OrganizationId",
                        columns: x => new { x.IntegrationId, x.OrganizationId },
                        principalTable: "Integrations",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntegrationStackAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationStackAssignments_Stacks_StackId_OrganizationId",
                        columns: x => new { x.StackId, x.OrganizationId },
                        principalTable: "Stacks",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationModuleAssignments_Id",
                table: "IntegrationModuleAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationModuleAssignments_IntegrationId",
                table: "IntegrationModuleAssignments",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationModuleAssignments_IntegrationId_OrganizationId",
                table: "IntegrationModuleAssignments",
                columns: new[] { "IntegrationId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationModuleAssignments_ModuleId",
                table: "IntegrationModuleAssignments",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationModuleAssignments_ModuleId_IntegrationId_OrganizationId",
                table: "IntegrationModuleAssignments",
                columns: new[] { "ModuleId", "IntegrationId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationModuleAssignments_ModuleId_OrganizationId",
                table: "IntegrationModuleAssignments",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationModuleAssignments_OrganizationId",
                table: "IntegrationModuleAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationNamespaceAssignments_Id",
                table: "IntegrationNamespaceAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationNamespaceAssignments_IntegrationId",
                table: "IntegrationNamespaceAssignments",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationNamespaceAssignments_IntegrationId_OrganizationId",
                table: "IntegrationNamespaceAssignments",
                columns: new[] { "IntegrationId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationNamespaceAssignments_NamespaceId",
                table: "IntegrationNamespaceAssignments",
                column: "NamespaceId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationNamespaceAssignments_NamespaceId_IntegrationId_OrganizationId",
                table: "IntegrationNamespaceAssignments",
                columns: new[] { "NamespaceId", "IntegrationId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationNamespaceAssignments_NamespaceId_OrganizationId",
                table: "IntegrationNamespaceAssignments",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationNamespaceAssignments_OrganizationId",
                table: "IntegrationNamespaceAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationStackAssignments_Id",
                table: "IntegrationStackAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationStackAssignments_IntegrationId",
                table: "IntegrationStackAssignments",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationStackAssignments_IntegrationId_OrganizationId",
                table: "IntegrationStackAssignments",
                columns: new[] { "IntegrationId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationStackAssignments_OrganizationId",
                table: "IntegrationStackAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationStackAssignments_StackId",
                table: "IntegrationStackAssignments",
                column: "StackId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationStackAssignments_StackId_IntegrationId_OrganizationId",
                table: "IntegrationStackAssignments",
                columns: new[] { "StackId", "IntegrationId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationStackAssignments_StackId_OrganizationId",
                table: "IntegrationStackAssignments",
                columns: new[] { "StackId", "OrganizationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationModuleAssignments");

            migrationBuilder.DropTable(
                name: "IntegrationNamespaceAssignments");

            migrationBuilder.DropTable(
                name: "IntegrationStackAssignments");

            migrationBuilder.DropColumn(
                name: "IsAssignedToAllModules",
                table: "Integrations");
        }
    }
}
