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
    public partial class TransferArtefacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSource",
                table: "TransferMigrateSagas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TransferArtefacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceFragmentCiphertext = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceFragmentMetaCiphertext = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceiverOutputsCiphertext = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_TransferArtefacts", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_TransferArtefacts_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransferArtefacts_Transfers_TransferId_OrganizationId",
                        columns: x => new { x.TransferId, x.OrganizationId },
                        principalTable: "Transfers",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransferArtefacts_Id",
                table: "TransferArtefacts",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferArtefacts_OrganizationId",
                table: "TransferArtefacts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferArtefacts_TransferId_OrganizationId",
                table: "TransferArtefacts",
                columns: new[] { "TransferId", "OrganizationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransferArtefacts");

            migrationBuilder.DropColumn(
                name: "IsSource",
                table: "TransferMigrateSagas");
        }
    }
}
