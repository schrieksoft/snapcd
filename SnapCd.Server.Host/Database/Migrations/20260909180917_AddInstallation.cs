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
    public partial class AddInstallation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Installations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    SeedGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LatestKnownVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LatestKnownVersionCheckedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTelemetryReportAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WhatsNewJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installations", x => x.Id);
                    table.CheckConstraint("CK_Installations_Singleton", "[Id] = 1");
                });

            // The one row every installation has; the seed follows the database from here on.
            migrationBuilder.Sql("INSERT INTO [Installations] ([Id], [SeedGuid], [CreatedAtUtc]) VALUES (1, NEWID(), SYSUTCDATETIME())");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Installations");
        }
    }
}
