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
    public partial class ProbeJobReferencesAreForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // These columns named a job without a key behind them, so nothing stopped one naming a
            // job that is not there. A row whose probe job is gone recorded a contribution that no
            // longer has an owner, and the keys below refuse it.
            //
            // The attempts come first: an attempt points at the sets under a restriction.
            migrationBuilder.Sql("""
                DELETE a FROM ModuleProbeAttempts a
                WHERE a.ProbeModuleJobId IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM ModuleJobs j
                                  WHERE j.Id = a.ProbeModuleJobId AND j.OrganizationId = a.OrganizationId);
                """);

            migrationBuilder.Sql("""
                DELETE s FROM SignalOutputSets s
                WHERE NOT EXISTS (SELECT 1 FROM ModuleJobs j
                                  WHERE j.Id = s.ModuleJobId AND j.OrganizationId = s.OrganizationId);
                """);

            migrationBuilder.Sql("""
                DELETE s FROM ResourceChangeSets s
                WHERE NOT EXISTS (SELECT 1 FROM ModuleJobs j
                                  WHERE j.Id = s.ModuleJobId AND j.OrganizationId = s.OrganizationId);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutputSets_ModuleJobId_OrganizationId",
                table: "SignalOutputSets",
                columns: new[] { "ModuleJobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceChangeSets_ModuleJobId_OrganizationId",
                table: "ResourceChangeSets",
                columns: new[] { "ModuleJobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleProbeAttempts_ProbeModuleJobId_OrganizationId",
                table: "ModuleProbeAttempts",
                columns: new[] { "ProbeModuleJobId", "OrganizationId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ModuleProbeAttempts_ModuleJobs_ProbeModuleJobId_OrganizationId",
                table: "ModuleProbeAttempts",
                columns: new[] { "ProbeModuleJobId", "OrganizationId" },
                principalTable: "ModuleJobs",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceChangeSets_ModuleJobs_ModuleJobId_OrganizationId",
                table: "ResourceChangeSets",
                columns: new[] { "ModuleJobId", "OrganizationId" },
                principalTable: "ModuleJobs",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SignalOutputSets_ModuleJobs_ModuleJobId_OrganizationId",
                table: "SignalOutputSets",
                columns: new[] { "ModuleJobId", "OrganizationId" },
                principalTable: "ModuleJobs",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModuleProbeAttempts_ModuleJobs_ProbeModuleJobId_OrganizationId",
                table: "ModuleProbeAttempts");

            migrationBuilder.DropForeignKey(
                name: "FK_ResourceChangeSets_ModuleJobs_ModuleJobId_OrganizationId",
                table: "ResourceChangeSets");

            migrationBuilder.DropForeignKey(
                name: "FK_SignalOutputSets_ModuleJobs_ModuleJobId_OrganizationId",
                table: "SignalOutputSets");

            migrationBuilder.DropIndex(
                name: "IX_SignalOutputSets_ModuleJobId_OrganizationId",
                table: "SignalOutputSets");

            migrationBuilder.DropIndex(
                name: "IX_ResourceChangeSets_ModuleJobId_OrganizationId",
                table: "ResourceChangeSets");

            migrationBuilder.DropIndex(
                name: "IX_ModuleProbeAttempts_ProbeModuleJobId_OrganizationId",
                table: "ModuleProbeAttempts");
        }
    }
}
