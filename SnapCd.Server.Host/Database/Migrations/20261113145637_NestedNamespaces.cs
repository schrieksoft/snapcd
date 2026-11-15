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
    public partial class NestedNamespaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultOutputSetDownstreamRestriction",
                table: "Namespaces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "Namespaces",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentNamespaceId",
                table: "Namespaces",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideOnApplyApprovalThreshold",
                table: "Namespaces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideOnApprovalTimeoutMinutes",
                table: "Namespaces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideOnCleanInitEnabled",
                table: "Namespaces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideOnDestroyApprovalThreshold",
                table: "Namespaces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideOnDriftCheckEnabled",
                table: "Namespaces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideOnDriftCheckIntervalMinutes",
                table: "Namespaces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideOnEngine",
                table: "Namespaces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideOnOutputRestrictionExemption",
                table: "Namespaces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideOnOutputSetDownstreamRestriction",
                table: "Namespaces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideOnStateMigrationApprovalThreshold",
                table: "Namespaces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideOnTriggerPathFilterEnabled",
                table: "Namespaces",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NamespaceAncestors",
                columns: table => new
                {
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AncestorNamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Depth = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NamespaceAncestors", x => new { x.NamespaceId, x.AncestorNamespaceId, x.OrganizationId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_Namespaces_Number",
                table: "Namespaces",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Namespaces_ParentNamespaceId_OrganizationId",
                table: "Namespaces",
                columns: new[] { "ParentNamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NsAnc_AncestorId_OrgId",
                table: "NamespaceAncestors",
                columns: new[] { "AncestorNamespaceId", "OrganizationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Namespaces_Namespaces_ParentNamespaceId_OrganizationId",
                table: "Namespaces",
                columns: new[] { "ParentNamespaceId", "OrganizationId" },
                principalTable: "Namespaces",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Namespaces_Namespaces_ParentNamespaceId_OrganizationId",
                table: "Namespaces");

            migrationBuilder.DropTable(
                name: "NamespaceAncestors");

            migrationBuilder.DropIndex(
                name: "IX_Namespaces_Number",
                table: "Namespaces");

            migrationBuilder.DropIndex(
                name: "IX_Namespaces_ParentNamespaceId_OrganizationId",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultOutputSetDownstreamRestriction",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "ParentNamespaceId",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "OverrideOnApplyApprovalThreshold",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "OverrideOnApprovalTimeoutMinutes",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "OverrideOnCleanInitEnabled",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "OverrideOnDestroyApprovalThreshold",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "OverrideOnDriftCheckEnabled",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "OverrideOnDriftCheckIntervalMinutes",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "OverrideOnEngine",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "OverrideOnOutputRestrictionExemption",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "OverrideOnOutputSetDownstreamRestriction",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "OverrideOnStateMigrationApprovalThreshold",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "OverrideOnTriggerPathFilterEnabled",
                table: "Namespaces");
        }
    }
}
