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

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SnapCd.Server.Host.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Integrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IntegrationType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Integrations", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_Integrations_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_Id",
                table: "Integrations",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_OrganizationId_IntegrationType_Name",
                table: "Integrations",
                columns: new[] { "OrganizationId", "IntegrationType", "Name" },
                unique: true);
        

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
        

            migrationBuilder.CreateTable(
                name: "IntegrationRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, computedColumnSql: "CASE WHEN [PrincipalDiscriminator] = 'User' THEN [UserId] WHEN [PrincipalDiscriminator] = 'ServicePrincipal' THEN [ServicePrincipalId] WHEN [PrincipalDiscriminator] = 'Group' THEN [GroupId] END", stored: true),
                    PrincipalDiscriminator = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GroupOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServicePrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationUserUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationUserOrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_IntegrationRoleAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_IntegrationRoleAssignments_Groups_GroupId_GroupOrganizationId",
                        columns: x => new { x.GroupId, x.GroupOrganizationId },
                        principalTable: "Groups",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntegrationRoleAssignments_Integrations_IntegrationId_OrganizationId",
                        columns: x => new { x.IntegrationId, x.OrganizationId },
                        principalTable: "Integrations",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntegrationRoleAssignments_OrganizationUsers_OrganizationUserUserId_OrganizationUserOrganizationId",
                        columns: x => new { x.OrganizationUserUserId, x.OrganizationUserOrganizationId },
                        principalTable: "OrganizationUsers",
                        principalColumns: new[] { "UserId", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntegrationRoleAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IntegrationRoleAssignments_ServicePrincipals_ServicePrincipalId",
                        column: x => x.ServicePrincipalId,
                        principalTable: "ServicePrincipals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_GroupId",
                table: "IntegrationRoleAssignments",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_GroupId_GroupOrganizationId",
                table: "IntegrationRoleAssignments",
                columns: new[] { "GroupId", "GroupOrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_GroupId_IntegrationId_OrganizationId_RoleName",
                table: "IntegrationRoleAssignments",
                columns: new[] { "GroupId", "IntegrationId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[GroupId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_Id",
                table: "IntegrationRoleAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_IntegrationId",
                table: "IntegrationRoleAssignments",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_IntegrationId_OrganizationId",
                table: "IntegrationRoleAssignments",
                columns: new[] { "IntegrationId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_OrganizationId",
                table: "IntegrationRoleAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_OrganizationUserUserId_OrganizationUserOrganizationId",
                table: "IntegrationRoleAssignments",
                columns: new[] { "OrganizationUserUserId", "OrganizationUserOrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_PrincipalId",
                table: "IntegrationRoleAssignments",
                column: "PrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_ServicePrincipalId",
                table: "IntegrationRoleAssignments",
                column: "ServicePrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_ServicePrincipalId_IntegrationId_OrganizationId_RoleName",
                table: "IntegrationRoleAssignments",
                columns: new[] { "ServicePrincipalId", "IntegrationId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[ServicePrincipalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_UserId",
                table: "IntegrationRoleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_UserId_IntegrationId_OrganizationId_RoleName",
                table: "IntegrationRoleAssignments",
                columns: new[] { "UserId", "IntegrationId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[UserId] IS NOT NULL");
        

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
        

            migrationBuilder.CreateTable(
                name: "IntegrationDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IntegrationEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Trigger = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModuleJobMissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DedupeKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationDeliveries", x => new { x.Id, x.OrganizationId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDeliveries_DedupeKey_IntegrationEventId_OrganizationId",
                table: "IntegrationDeliveries",
                columns: new[] { "DedupeKey", "IntegrationEventId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDeliveries_Id",
                table: "IntegrationDeliveries",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDeliveries_IntegrationId",
                table: "IntegrationDeliveries",
                column: "IntegrationId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationDeliveries_IntegrationId_ModuleJobMissionId_OrganizationId",
                table: "IntegrationDeliveries",
                columns: new[] { "IntegrationId", "ModuleJobMissionId", "OrganizationId" });
        
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationDeliveries");
        

            migrationBuilder.DropTable(
                name: "ModuleIntegrationEvents");

            migrationBuilder.DropTable(
                name: "NamespaceIntegrationEvents");

            migrationBuilder.DropTable(
                name: "OrganizationIntegrationEvents");

            migrationBuilder.DropTable(
                name: "StackIntegrationEvents");
        

            migrationBuilder.DropTable(
                name: "IntegrationRoleAssignments");
        

            migrationBuilder.DropTable(
                name: "IntegrationModuleAssignments");

            migrationBuilder.DropTable(
                name: "IntegrationNamespaceAssignments");

            migrationBuilder.DropTable(
                name: "IntegrationStackAssignments");

            migrationBuilder.DropColumn(
                name: "IsAssignedToAllModules",
                table: "Integrations");
        

            migrationBuilder.DropTable(
                name: "Integrations");
        
        }
    }
}
