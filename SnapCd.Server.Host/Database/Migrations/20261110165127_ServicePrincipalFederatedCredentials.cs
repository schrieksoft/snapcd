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
    public partial class ServicePrincipalFederatedCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServicePrincipalFederatedCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicePrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Issuer = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Audience = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
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
                    table.PrimaryKey("PK_ServicePrincipalFederatedCredentials", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ServicePrincipalFederatedCredentials_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServicePrincipalFederatedCredentials_ServicePrincipals_ServicePrincipalId_OrganizationId",
                        columns: x => new { x.ServicePrincipalId, x.OrganizationId },
                        principalTable: "ServicePrincipals",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipalFederatedCredentials_Id",
                table: "ServicePrincipalFederatedCredentials",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipalFederatedCredentials_Issuer_Subject",
                table: "ServicePrincipalFederatedCredentials",
                columns: new[] { "Issuer", "Subject" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipalFederatedCredentials_Issuer_Subject_ServicePrincipalId_OrganizationId",
                table: "ServicePrincipalFederatedCredentials",
                columns: new[] { "Issuer", "Subject", "ServicePrincipalId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipalFederatedCredentials_Name_ServicePrincipalId_OrganizationId",
                table: "ServicePrincipalFederatedCredentials",
                columns: new[] { "Name", "ServicePrincipalId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipalFederatedCredentials_OrganizationId",
                table: "ServicePrincipalFederatedCredentials",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipalFederatedCredentials_ServicePrincipalId",
                table: "ServicePrincipalFederatedCredentials",
                column: "ServicePrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrincipalFederatedCredentials_ServicePrincipalId_OrganizationId",
                table: "ServicePrincipalFederatedCredentials",
                columns: new[] { "ServicePrincipalId", "OrganizationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServicePrincipalFederatedCredentials");
        }
    }
}
