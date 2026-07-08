// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnapCd.Server.Host.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRecursiveGroupMemberForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecursiveGroupMembers_Groups_GroupId_OrganizationId",
                table: "RecursiveGroupMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_RecursiveGroupMembers_Groups_RootGroupId_RootOrganizationId",
                table: "RecursiveGroupMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_RecursiveGroupMembers_Organizations_OrganizationId",
                table: "RecursiveGroupMembers");

            migrationBuilder.DropIndex(
                name: "IX_RecursiveGroupMembers_OrganizationId",
                table: "RecursiveGroupMembers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RecursiveGroupMembers_OrganizationId",
                table: "RecursiveGroupMembers",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecursiveGroupMembers_Groups_GroupId_OrganizationId",
                table: "RecursiveGroupMembers",
                columns: new[] { "GroupId", "OrganizationId" },
                principalTable: "Groups",
                principalColumns: new[] { "Id", "OrganizationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_RecursiveGroupMembers_Groups_RootGroupId_RootOrganizationId",
                table: "RecursiveGroupMembers",
                columns: new[] { "RootGroupId", "RootOrganizationId" },
                principalTable: "Groups",
                principalColumns: new[] { "Id", "OrganizationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_RecursiveGroupMembers_Organizations_OrganizationId",
                table: "RecursiveGroupMembers",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id");
        }
    }
}
