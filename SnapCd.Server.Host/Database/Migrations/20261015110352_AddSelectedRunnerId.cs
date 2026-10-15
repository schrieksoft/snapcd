// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using SnapCd.Server.Host.Database.Migrations.Sql;

#nullable disable

namespace SnapCd.Server.Host.Database.Migrations
{
    /// <inheritdoc />
    /// <inheritdoc />
    /// <summary>
    /// Records on each job row which runner it was dispatched to. The saga already holds it and is
    /// deleted when the job finishes, so the row is where the fact has to live for anything that
    /// authorizes a runner against a job. No foreign key: it is a record of what happened, and it
    /// outlives the runner.
    /// </summary>
    public partial class AddSelectedRunnerId : Migration
    {
        private const string SqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261015110352_AddSelectedRunnerId.sql";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SelectedRunnerId",
                table: "StateMigrationJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SelectedRunnerId",
                table: "RippleProbeJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SelectedRunnerId",
                table: "ModuleJobs",
                type: "uniqueidentifier",
                nullable: true);

            foreach (var batch in MigrationSql.ReadBatches(SqlResource))
            {
                migrationBuilder.Sql(batch);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedRunnerId",
                table: "StateMigrationJobs");

            migrationBuilder.DropColumn(
                name: "SelectedRunnerId",
                table: "RippleProbeJobs");

            migrationBuilder.DropColumn(
                name: "SelectedRunnerId",
                table: "ModuleJobs");
        }
    }
}
