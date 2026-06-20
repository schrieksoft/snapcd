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
    public partial class AddIntegrationRoleAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationRoleAssignments");
        }
    }
}
