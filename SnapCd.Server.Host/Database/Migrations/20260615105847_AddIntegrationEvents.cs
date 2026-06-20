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
    public partial class AddIntegrationEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModuleIntegrationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Trigger = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Template = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    Filter = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ModuleIntegrationEvents", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleIntegrationEvents_Integrations_IntegrationId_OrganizationId",
                        columns: x => new { x.IntegrationId, x.OrganizationId },
                        principalTable: "Integrations",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleIntegrationEvents_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleIntegrationEvents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceIntegrationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Trigger = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Template = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    Filter = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_NamespaceIntegrationEvents", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_NamespaceIntegrationEvents_Integrations_IntegrationId_OrganizationId",
                        columns: x => new { x.IntegrationId, x.OrganizationId },
                        principalTable: "Integrations",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceIntegrationEvents_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceIntegrationEvents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationIntegrationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Trigger = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Template = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    Filter = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_OrganizationIntegrationEvents", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_OrganizationIntegrationEvents_Integrations_IntegrationId_OrganizationId",
                        columns: x => new { x.IntegrationId, x.OrganizationId },
                        principalTable: "Integrations",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationIntegrationEvents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StackIntegrationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Trigger = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Template = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    Filter = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_StackIntegrationEvents", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_StackIntegrationEvents_Integrations_IntegrationId_OrganizationId",
                        columns: x => new { x.IntegrationId, x.OrganizationId },
                        principalTable: "Integrations",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StackIntegrationEvents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StackIntegrationEvents_Stacks_StackId_OrganizationId",
                        columns: x => new { x.StackId, x.OrganizationId },
                        principalTable: "Stacks",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleIntegrationEvents_Id",
                table: "ModuleIntegrationEvents",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleIntegrationEvents_IntegrationId",
                table: "ModuleIntegrationEvents",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleIntegrationEvents_IntegrationId_OrganizationId",
                table: "ModuleIntegrationEvents",
                columns: new[] { "IntegrationId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleIntegrationEvents_ModuleId",
                table: "ModuleIntegrationEvents",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleIntegrationEvents_ModuleId_IntegrationId_OrganizationId_Trigger",
                table: "ModuleIntegrationEvents",
                columns: new[] { "ModuleId", "IntegrationId", "OrganizationId", "Trigger" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleIntegrationEvents_ModuleId_OrganizationId",
                table: "ModuleIntegrationEvents",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleIntegrationEvents_OrganizationId",
                table: "ModuleIntegrationEvents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceIntegrationEvents_Id",
                table: "NamespaceIntegrationEvents",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceIntegrationEvents_IntegrationId",
                table: "NamespaceIntegrationEvents",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceIntegrationEvents_IntegrationId_OrganizationId",
                table: "NamespaceIntegrationEvents",
                columns: new[] { "IntegrationId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceIntegrationEvents_NamespaceId",
                table: "NamespaceIntegrationEvents",
                column: "NamespaceId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceIntegrationEvents_NamespaceId_IntegrationId_OrganizationId_Trigger",
                table: "NamespaceIntegrationEvents",
                columns: new[] { "NamespaceId", "IntegrationId", "OrganizationId", "Trigger" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceIntegrationEvents_NamespaceId_OrganizationId",
                table: "NamespaceIntegrationEvents",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceIntegrationEvents_OrganizationId",
                table: "NamespaceIntegrationEvents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationIntegrationEvents_Id",
                table: "OrganizationIntegrationEvents",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationIntegrationEvents_IntegrationId",
                table: "OrganizationIntegrationEvents",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationIntegrationEvents_IntegrationId_OrganizationId_Trigger",
                table: "OrganizationIntegrationEvents",
                columns: new[] { "IntegrationId", "OrganizationId", "Trigger" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationIntegrationEvents_OrganizationId",
                table: "OrganizationIntegrationEvents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StackIntegrationEvents_Id",
                table: "StackIntegrationEvents",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StackIntegrationEvents_IntegrationId",
                table: "StackIntegrationEvents",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_StackIntegrationEvents_IntegrationId_OrganizationId",
                table: "StackIntegrationEvents",
                columns: new[] { "IntegrationId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StackIntegrationEvents_OrganizationId",
                table: "StackIntegrationEvents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StackIntegrationEvents_StackId",
                table: "StackIntegrationEvents",
                column: "StackId");

            migrationBuilder.CreateIndex(
                name: "IX_StackIntegrationEvents_StackId_IntegrationId_OrganizationId_Trigger",
                table: "StackIntegrationEvents",
                columns: new[] { "StackId", "IntegrationId", "OrganizationId", "Trigger" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StackIntegrationEvents_StackId_OrganizationId",
                table: "StackIntegrationEvents",
                columns: new[] { "StackId", "OrganizationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModuleIntegrationEvents");

            migrationBuilder.DropTable(
                name: "NamespaceIntegrationEvents");

            migrationBuilder.DropTable(
                name: "OrganizationIntegrationEvents");

            migrationBuilder.DropTable(
                name: "StackIntegrationEvents");
        }
    }
}
