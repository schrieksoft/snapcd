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
    public partial class AddIntegrationDeliveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
