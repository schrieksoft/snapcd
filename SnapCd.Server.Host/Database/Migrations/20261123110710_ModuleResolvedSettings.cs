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
    /// <summary>
    /// The table that holds what each inheritable setting resolves to per Module, with the
    /// procedure and triggers that keep it current. It replaces the NamespaceResolvedDefaults view,
    /// and replaces the ancestry closure's rebuild with one that writes only the rows that differ.
    /// </summary>
    public partial class ModuleResolvedSettings : Migration
    {
        private const string SqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261123110710_ModuleResolvedSettings.sql";

        private const string DownSqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261123110710_ModuleResolvedSettings.Down.sql";

        private const string ReplacedViewSqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261113180000_NamespaceResolvedDefaults.sql";

        private const string ReplacedClosureSqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261113150000_NamespaceAncestorTriggers.sql";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModuleResolvedSettings",
                columns: table => new
                {
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CleanInitEnabled = table.Column<bool>(type: "bit", nullable: true),
                    TriggerPathFilterEnabled = table.Column<bool>(type: "bit", nullable: true),
                    ApplyApprovalThreshold = table.Column<int>(type: "int", nullable: true),
                    DestroyApprovalThreshold = table.Column<int>(type: "int", nullable: true),
                    StateMigrationApprovalThreshold = table.Column<int>(type: "int", nullable: true),
                    ApprovalTimeoutMinutes = table.Column<int>(type: "int", nullable: true),
                    Engine = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DriftCheckEnabled = table.Column<bool>(type: "bit", nullable: true),
                    DriftCheckIntervalMinutes = table.Column<int>(type: "int", nullable: true),
                    OutputSetDownstreamRestriction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OutputRestrictionExemption = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OutputRestrictionExemptionNamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleResolvedSettings", x => new { x.ModuleId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleResolvedSettings_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleResolvedSettings_Namespaces_OutputRestrictionExemptionNamespaceId_OrganizationId",
                        columns: x => new { x.OutputRestrictionExemptionNamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleResolvedSettings_OutputRestrictionExemptionNamespaceId_OrganizationId",
                table: "ModuleResolvedSettings",
                columns: new[] { "OutputRestrictionExemptionNamespaceId", "OrganizationId" });

            foreach (var batch in MigrationSql.ReadBatches(SqlResource))
            {
                migrationBuilder.Sql(batch);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var batch in MigrationSql.ReadBatches(DownSqlResource))
            {
                migrationBuilder.Sql(batch);
            }

            foreach (var batch in MigrationSql.ReadBatches(ReplacedClosureSqlResource))
            {
                migrationBuilder.Sql(batch);
            }

            foreach (var batch in MigrationSql.ReadBatches(ReplacedViewSqlResource))
            {
                migrationBuilder.Sql(batch);
            }

            migrationBuilder.DropTable(
                name: "ModuleResolvedSettings");
        }
    }
}
