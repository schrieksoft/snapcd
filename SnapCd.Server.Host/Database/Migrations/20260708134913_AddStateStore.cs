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
    public partial class AddStateStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StateStores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
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
                    table.PrimaryKey("PK_StateStores", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_StateStores_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StateFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StateStoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    LockId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LockInfo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LockCreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LockedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
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
                    table.PrimaryKey("PK_StateFiles", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_StateFiles_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateFiles_StateStores_StateStoreId_OrganizationId",
                        columns: x => new { x.StateStoreId, x.OrganizationId },
                        principalTable: "StateStores",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateStoreRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StateStoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, computedColumnSql: "CASE WHEN [PrincipalDiscriminator] = 'User' THEN [UserId] WHEN [PrincipalDiscriminator] = 'ServicePrincipal' THEN [ServicePrincipalId] WHEN [PrincipalDiscriminator] = 'Group' THEN [GroupId] END", stored: true),
                    PrincipalDiscriminator = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServicePrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_StateStoreRoleAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_StateStoreRoleAssignments_Groups_GroupId_OrganizationId",
                        columns: x => new { x.GroupId, x.OrganizationId },
                        principalTable: "Groups",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StateStoreRoleAssignments_OrganizationUsers_UserId_OrganizationId",
                        columns: x => new { x.UserId, x.OrganizationId },
                        principalTable: "OrganizationUsers",
                        principalColumns: new[] { "UserId", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StateStoreRoleAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateStoreRoleAssignments_ServicePrincipals_ServicePrincipalId_OrganizationId",
                        columns: x => new { x.ServicePrincipalId, x.OrganizationId },
                        principalTable: "ServicePrincipals",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StateStoreRoleAssignments_StateStores_StateStoreId_OrganizationId",
                        columns: x => new { x.StateStoreId, x.OrganizationId },
                        principalTable: "StateStores",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateFileVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StateFileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateFileVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StateFileVersions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StateFileVersions_StateFiles_StateFileId_OrganizationId",
                        columns: x => new { x.StateFileId, x.OrganizationId },
                        principalTable: "StateFiles",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StateFiles_Id",
                table: "StateFiles",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateFiles_OrganizationId",
                table: "StateFiles",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StateFiles_StateStoreId_Name",
                table: "StateFiles",
                columns: new[] { "StateStoreId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateFiles_StateStoreId_OrganizationId",
                table: "StateFiles",
                columns: new[] { "StateStoreId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StateFileVersions_OrganizationId",
                table: "StateFileVersions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StateFileVersions_StateFileId_CreatedDateTime",
                table: "StateFileVersions",
                columns: new[] { "StateFileId", "CreatedDateTime" },
                descending: new[] { false, true })
                .Annotation("SqlServer:Include", new[] { "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_StateFileVersions_StateFileId_OrganizationId",
                table: "StateFileVersions",
                columns: new[] { "StateFileId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StateStoreRoleAssign_Group_PrincipalFirst",
                table: "StateStoreRoleAssignments",
                columns: new[] { "PrincipalId", "StateStoreId", "OrganizationId", "RoleName" },
                filter: "[PrincipalDiscriminator] = 'Group'");

            migrationBuilder.CreateIndex(
                name: "IX_StateStoreRoleAssign_UserSP_StoreFirst",
                table: "StateStoreRoleAssignments",
                columns: new[] { "StateStoreId", "OrganizationId", "PrincipalId", "RoleName" },
                filter: "[PrincipalDiscriminator] IN ('User', 'ServicePrincipal')");

            migrationBuilder.CreateIndex(
                name: "IX_StateStoreRoleAssignments_GroupId",
                table: "StateStoreRoleAssignments",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_StateStoreRoleAssignments_GroupId_OrganizationId",
                table: "StateStoreRoleAssignments",
                columns: new[] { "GroupId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StateStoreRoleAssignments_GroupId_StateStoreId_OrganizationId_RoleName",
                table: "StateStoreRoleAssignments",
                columns: new[] { "GroupId", "StateStoreId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[GroupId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StateStoreRoleAssignments_Id",
                table: "StateStoreRoleAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateStoreRoleAssignments_OrganizationId",
                table: "StateStoreRoleAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StateStoreRoleAssignments_PrincipalId",
                table: "StateStoreRoleAssignments",
                column: "PrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_StateStoreRoleAssignments_ServicePrincipalId",
                table: "StateStoreRoleAssignments",
                column: "ServicePrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_StateStoreRoleAssignments_ServicePrincipalId_OrganizationId",
                table: "StateStoreRoleAssignments",
                columns: new[] { "ServicePrincipalId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StateStoreRoleAssignments_ServicePrincipalId_StateStoreId_OrganizationId_RoleName",
                table: "StateStoreRoleAssignments",
                columns: new[] { "ServicePrincipalId", "StateStoreId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[ServicePrincipalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StateStoreRoleAssignments_UserId",
                table: "StateStoreRoleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StateStoreRoleAssignments_UserId_OrganizationId",
                table: "StateStoreRoleAssignments",
                columns: new[] { "UserId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StateStoreRoleAssignments_UserId_StateStoreId_OrganizationId_RoleName",
                table: "StateStoreRoleAssignments",
                columns: new[] { "UserId", "StateStoreId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StateStores_CreatedDateTime",
                table: "StateStores",
                column: "CreatedDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_StateStores_Id",
                table: "StateStores",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateStores_OrganizationId_Name",
                table: "StateStores",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StateFileVersions");

            migrationBuilder.DropTable(
                name: "StateStoreRoleAssignments");

            migrationBuilder.DropTable(
                name: "StateFiles");

            migrationBuilder.DropTable(
                name: "StateStores");
        }
    }
}
