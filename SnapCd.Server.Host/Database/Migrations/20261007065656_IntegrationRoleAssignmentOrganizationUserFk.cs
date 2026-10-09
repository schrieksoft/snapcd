// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SnapCd.Server.Host.Database.Migrations
{
    /// <inheritdoc />
    public partial class IntegrationRoleAssignmentOrganizationUserFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // OrganizationUserUserId is what the old FK enforced, so it is the authoritative
            // value: copy it onto UserId before the column goes. Rows that never had it set are
            // left alone, and any row still pointing at no OrganizationUser is deleted rather
            // than left to fail the new constraint, since it grants nothing either way.
            migrationBuilder.Sql(@"
                UPDATE [IntegrationRoleAssignments]
                SET [UserId] = [OrganizationUserUserId]
                WHERE [OrganizationUserUserId] IS NOT NULL;

                DELETE ra
                FROM [IntegrationRoleAssignments] ra
                WHERE ra.[UserId] IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM [OrganizationUsers] ou
                      WHERE ou.[UserId] = ra.[UserId]
                        AND ou.[OrganizationId] = ra.[OrganizationId]);
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_IntegrationRoleAssignments_OrganizationUsers_OrganizationUserUserId_OrganizationUserOrganizationId",
                table: "IntegrationRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationRoleAssignments_OrganizationUserUserId_OrganizationUserOrganizationId",
                table: "IntegrationRoleAssignments");

            migrationBuilder.DropColumn(
                name: "OrganizationUserOrganizationId",
                table: "IntegrationRoleAssignments");

            migrationBuilder.DropColumn(
                name: "OrganizationUserUserId",
                table: "IntegrationRoleAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_UserId_OrganizationId",
                table: "IntegrationRoleAssignments",
                columns: new[] { "UserId", "OrganizationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationRoleAssignments_OrganizationUsers_UserId_OrganizationId",
                table: "IntegrationRoleAssignments",
                columns: new[] { "UserId", "OrganizationId" },
                principalTable: "OrganizationUsers",
                principalColumns: new[] { "UserId", "OrganizationId" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_IntegrationRoleAssignments_OrganizationUsers_UserId_OrganizationId",
                table: "IntegrationRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationRoleAssignments_UserId_OrganizationId",
                table: "IntegrationRoleAssignments");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationUserOrganizationId",
                table: "IntegrationRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationUserUserId",
                table: "IntegrationRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_OrganizationUserUserId_OrganizationUserOrganizationId",
                table: "IntegrationRoleAssignments",
                columns: new[] { "OrganizationUserUserId", "OrganizationUserOrganizationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_IntegrationRoleAssignments_OrganizationUsers_OrganizationUserUserId_OrganizationUserOrganizationId",
                table: "IntegrationRoleAssignments",
                columns: new[] { "OrganizationUserUserId", "OrganizationUserOrganizationId" },
                principalTable: "OrganizationUsers",
                principalColumns: new[] { "UserId", "OrganizationId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
