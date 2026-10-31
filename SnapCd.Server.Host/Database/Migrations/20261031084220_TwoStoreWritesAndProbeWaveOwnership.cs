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
    public partial class TwoStoreWritesAndProbeWaveOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResourceChangeSets_Modules_ModuleId_OrganizationId",
                table: "ResourceChangeSets");

            migrationBuilder.DropForeignKey(
                name: "FK_SignalOutputSets_Modules_ModuleId_OrganizationId",
                table: "SignalOutputSets");

            migrationBuilder.DropIndex(
                name: "IX_SignalOutputSets_ProbeWaveId",
                table: "SignalOutputSets");

            migrationBuilder.DropIndex(
                name: "IX_ResourceChangeSets_ProbeWaveId",
                table: "ResourceChangeSets");

            migrationBuilder.AddColumn<DateTime>(
                name: "RemoteValueMissingSince",
                table: "SignalOutputs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NameContainsScope",
                table: "Secrets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RemoteValueMissingSince",
                table: "Secrets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RemoteValueMissingSince",
                table: "Outputs",
                type: "datetime2",
                nullable: true);

            // Two columns recorded a wave and a job without a key behind them, so a wave or job
            // could go without the rows that named it. The keys below refuse a row naming one that
            // is not there, and such a row has nothing left to be an answer to: the episode that
            // asked the question is gone.
            //
            // The attempts come first throughout. An attempt points at both sets under a
            // restriction that is not dropped here, so a doomed set cannot go while one names it.
            migrationBuilder.Sql("""
                DELETE a FROM ModuleProbeAttempts a
                INNER JOIN SignalOutputSets s
                    ON s.Id = a.SignalOutputSetId AND s.OrganizationId = a.OrganizationId
                WHERE NOT EXISTS (SELECT 1 FROM ProbeWaves w
                                  WHERE w.Id = s.ProbeWaveId AND w.OrganizationId = s.OrganizationId);
                """);

            migrationBuilder.Sql("""
                DELETE a FROM ModuleProbeAttempts a
                INNER JOIN ResourceChangeSets s
                    ON s.Id = a.ResourceChangeSetId AND s.OrganizationId = a.OrganizationId
                WHERE NOT EXISTS (SELECT 1 FROM ProbeWaves w
                                  WHERE w.Id = s.ProbeWaveId AND w.OrganizationId = s.OrganizationId);
                """);

            migrationBuilder.Sql("""
                DELETE a FROM ModuleProbeAttempts a
                INNER JOIN SignalOutputSets s
                    ON s.Id = a.SignalOutputSetId AND s.OrganizationId = a.OrganizationId
                WHERE NOT EXISTS (SELECT 1 FROM ModuleJobs j
                                  WHERE j.Id = s.ModuleJobId AND j.OrganizationId = s.OrganizationId);
                """);

            migrationBuilder.Sql("""
                DELETE a FROM ModuleProbeAttempts a
                INNER JOIN ResourceChangeSets s
                    ON s.Id = a.ResourceChangeSetId AND s.OrganizationId = a.OrganizationId
                WHERE NOT EXISTS (SELECT 1 FROM ModuleJobs j
                                  WHERE j.Id = s.ModuleJobId AND j.OrganizationId = s.OrganizationId);
                """);

            migrationBuilder.Sql("""
                DELETE a FROM ModuleProbeAttempts a
                WHERE a.ProbeModuleJobId IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM ModuleJobs j
                                  WHERE j.Id = a.ProbeModuleJobId AND j.OrganizationId = a.OrganizationId);
                """);

            migrationBuilder.Sql("""
                DELETE s FROM SignalOutputSets s
                WHERE NOT EXISTS (SELECT 1 FROM ProbeWaves w
                                  WHERE w.Id = s.ProbeWaveId AND w.OrganizationId = s.OrganizationId)
                   OR NOT EXISTS (SELECT 1 FROM ModuleJobs j
                                  WHERE j.Id = s.ModuleJobId AND j.OrganizationId = s.OrganizationId);
                """);

            migrationBuilder.Sql("""
                DELETE s FROM ResourceChangeSets s
                WHERE NOT EXISTS (SELECT 1 FROM ProbeWaves w
                                  WHERE w.Id = s.ProbeWaveId AND w.OrganizationId = s.OrganizationId)
                   OR NOT EXISTS (SELECT 1 FROM ModuleJobs j
                                  WHERE j.Id = s.ModuleJobId AND j.OrganizationId = s.OrganizationId);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutputSets_ModuleJobId_OrganizationId",
                table: "SignalOutputSets",
                columns: new[] { "ModuleJobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutputSets_ProbeWaveId_OrganizationId",
                table: "SignalOutputSets",
                columns: new[] { "ProbeWaveId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceChangeSets_ModuleJobId_OrganizationId",
                table: "ResourceChangeSets",
                columns: new[] { "ModuleJobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceChangeSets_ProbeWaveId_OrganizationId",
                table: "ResourceChangeSets",
                columns: new[] { "ProbeWaveId", "OrganizationId" });

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
                name: "FK_ResourceChangeSets_Modules_ModuleId_OrganizationId",
                table: "ResourceChangeSets",
                columns: new[] { "ModuleId", "OrganizationId" },
                principalTable: "Modules",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceChangeSets_ProbeWaves_ProbeWaveId_OrganizationId",
                table: "ResourceChangeSets",
                columns: new[] { "ProbeWaveId", "OrganizationId" },
                principalTable: "ProbeWaves",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SignalOutputSets_ModuleJobs_ModuleJobId_OrganizationId",
                table: "SignalOutputSets",
                columns: new[] { "ModuleJobId", "OrganizationId" },
                principalTable: "ModuleJobs",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SignalOutputSets_Modules_ModuleId_OrganizationId",
                table: "SignalOutputSets",
                columns: new[] { "ModuleId", "OrganizationId" },
                principalTable: "Modules",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SignalOutputSets_ProbeWaves_ProbeWaveId_OrganizationId",
                table: "SignalOutputSets",
                columns: new[] { "ProbeWaveId", "OrganizationId" },
                principalTable: "ProbeWaves",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Cascade);
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
                name: "FK_ResourceChangeSets_Modules_ModuleId_OrganizationId",
                table: "ResourceChangeSets");

            migrationBuilder.DropForeignKey(
                name: "FK_ResourceChangeSets_ProbeWaves_ProbeWaveId_OrganizationId",
                table: "ResourceChangeSets");

            migrationBuilder.DropForeignKey(
                name: "FK_SignalOutputSets_ModuleJobs_ModuleJobId_OrganizationId",
                table: "SignalOutputSets");

            migrationBuilder.DropForeignKey(
                name: "FK_SignalOutputSets_Modules_ModuleId_OrganizationId",
                table: "SignalOutputSets");

            migrationBuilder.DropForeignKey(
                name: "FK_SignalOutputSets_ProbeWaves_ProbeWaveId_OrganizationId",
                table: "SignalOutputSets");

            migrationBuilder.DropIndex(
                name: "IX_SignalOutputSets_ModuleJobId_OrganizationId",
                table: "SignalOutputSets");

            migrationBuilder.DropIndex(
                name: "IX_SignalOutputSets_ProbeWaveId_OrganizationId",
                table: "SignalOutputSets");

            migrationBuilder.DropIndex(
                name: "IX_ResourceChangeSets_ModuleJobId_OrganizationId",
                table: "ResourceChangeSets");

            migrationBuilder.DropIndex(
                name: "IX_ResourceChangeSets_ProbeWaveId_OrganizationId",
                table: "ResourceChangeSets");

            migrationBuilder.DropIndex(
                name: "IX_ModuleProbeAttempts_ProbeModuleJobId_OrganizationId",
                table: "ModuleProbeAttempts");

            migrationBuilder.DropColumn(
                name: "RemoteValueMissingSince",
                table: "SignalOutputs");

            migrationBuilder.DropColumn(
                name: "NameContainsScope",
                table: "Secrets");

            migrationBuilder.DropColumn(
                name: "RemoteValueMissingSince",
                table: "Secrets");

            migrationBuilder.DropColumn(
                name: "RemoteValueMissingSince",
                table: "Outputs");

            migrationBuilder.CreateIndex(
                name: "IX_SignalOutputSets_ProbeWaveId",
                table: "SignalOutputSets",
                column: "ProbeWaveId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceChangeSets_ProbeWaveId",
                table: "ResourceChangeSets",
                column: "ProbeWaveId");

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceChangeSets_Modules_ModuleId_OrganizationId",
                table: "ResourceChangeSets",
                columns: new[] { "ModuleId", "OrganizationId" },
                principalTable: "Modules",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SignalOutputSets_Modules_ModuleId_OrganizationId",
                table: "SignalOutputSets",
                columns: new[] { "ModuleId", "OrganizationId" },
                principalTable: "Modules",
                principalColumns: new[] { "Id", "OrganizationId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
