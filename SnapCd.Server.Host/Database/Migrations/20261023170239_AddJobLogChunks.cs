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
    public partial class AddJobLogChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModuleJobLogChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchTimeStamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FirstTimestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastTimestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TaskNames = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LineCount = table.Column<int>(type: "int", nullable: false),
                    UncompressedBytes = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleJobLogChunks", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleJobLogChunks_ModuleJobs_JobId_OrganizationId",
                        columns: x => new { x.JobId, x.OrganizationId },
                        principalTable: "ModuleJobs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StateMigrationJobLogChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchTimeStamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FirstTimestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastTimestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TaskNames = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LineCount = table.Column<int>(type: "int", nullable: false),
                    UncompressedBytes = table.Column<int>(type: "int", nullable: false),
                    Payload = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StateMigrationJobLogChunks", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_StateMigrationJobLogChunks_StateMigrationJobs_JobId_OrganizationId",
                        columns: x => new { x.JobId, x.OrganizationId },
                        principalTable: "StateMigrationJobs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobLogChunks_Id",
                table: "ModuleJobLogChunks",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobLogChunks_JobId_ModuleId_BatchTimeStamp",
                table: "ModuleJobLogChunks",
                columns: new[] { "JobId", "ModuleId", "BatchTimeStamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobLogChunks_JobId_OrganizationId",
                table: "ModuleJobLogChunks",
                columns: new[] { "JobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobLogChunks_JobId_Sequence",
                table: "ModuleJobLogChunks",
                columns: new[] { "JobId", "Sequence" },
                unique: true)
                .Annotation("SqlServer:Include", new[] { "ModuleId", "TaskNames", "LineCount", "UncompressedBytes" });

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobLogChunks_Id",
                table: "StateMigrationJobLogChunks",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobLogChunks_JobId_ModuleId_BatchTimeStamp",
                table: "StateMigrationJobLogChunks",
                columns: new[] { "JobId", "ModuleId", "BatchTimeStamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobLogChunks_JobId_OrganizationId",
                table: "StateMigrationJobLogChunks",
                columns: new[] { "JobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StateMigrationJobLogChunks_JobId_Sequence",
                table: "StateMigrationJobLogChunks",
                columns: new[] { "JobId", "Sequence" },
                unique: true)
                .Annotation("SqlServer:Include", new[] { "ModuleId", "TaskNames", "LineCount", "UncompressedBytes" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModuleJobLogChunks");

            migrationBuilder.DropTable(
                name: "StateMigrationJobLogChunks");
        }
    }
}
