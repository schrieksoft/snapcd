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

            migrationBuilder.AddColumn<bool>(
                name: "RestrictOverrideOnApplyApprovalThreshold",
                table: "Namespaces",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RestrictOverrideOnApprovalTimeoutMinutes",
                table: "Namespaces",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RestrictOverrideOnCleanInitEnabled",
                table: "Namespaces",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RestrictOverrideOnDestroyApprovalThreshold",
                table: "Namespaces",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RestrictOverrideOnDriftCheckEnabled",
                table: "Namespaces",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RestrictOverrideOnDriftCheckIntervalMinutes",
                table: "Namespaces",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RestrictOverrideOnEngine",
                table: "Namespaces",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RestrictOverrideOnOutputSetDownstreamRestriction",
                table: "Namespaces",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RestrictOverrideOnStateMigrationApprovalThreshold",
                table: "Namespaces",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RestrictOverrideOnTriggerPathFilterEnabled",
                table: "Namespaces",
                type: "bit",
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
                name: "RestrictOverrideOnApplyApprovalThreshold",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "RestrictOverrideOnApprovalTimeoutMinutes",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "RestrictOverrideOnCleanInitEnabled",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "RestrictOverrideOnDestroyApprovalThreshold",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "RestrictOverrideOnDriftCheckEnabled",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "RestrictOverrideOnDriftCheckIntervalMinutes",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "RestrictOverrideOnEngine",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "RestrictOverrideOnOutputSetDownstreamRestriction",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "RestrictOverrideOnStateMigrationApprovalThreshold",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "RestrictOverrideOnTriggerPathFilterEnabled",
                table: "Namespaces");
        }
    }
}
