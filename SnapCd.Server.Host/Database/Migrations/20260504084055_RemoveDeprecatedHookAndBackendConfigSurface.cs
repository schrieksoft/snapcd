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
    public partial class RemoveDeprecatedHookAndBackendConfigSurface : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModuleBackendConfigs");

            migrationBuilder.DropTable(
                name: "NamespaceBackendConfigs");

            migrationBuilder.DropColumn(
                name: "DefaultApplyAfterHook",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultApplyBeforeHook",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultAutoMigrateEnabled",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultAutoReconfigureEnabled",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultAutoUpgradeEnabled",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultDestroyAfterHook",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultDestroyBeforeHook",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultInitAfterHook",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultInitBeforeHook",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultOutputAfterHook",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultOutputBeforeHook",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultPlanAfterHook",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultPlanBeforeHook",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultPlanDestroyAfterHook",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultPlanDestroyBeforeHook",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultValidateAfterHook",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "DefaultValidateBeforeHook",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "ApplyAfterHook",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "ApplyBeforeHook",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "AutoMigrateEnabled",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "AutoReconfigureEnabled",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "AutoUpgradeEnabled",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "DestroyAfterHook",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "DestroyBeforeHook",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "IgnoreNamespaceBackendConfigs",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "InitAfterHook",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "InitBeforeHook",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "OutputAfterHook",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "OutputBeforeHook",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "PlanAfterHook",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "PlanBeforeHook",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "PlanDestroyAfterHook",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "PlanDestroyBeforeHook",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "ValidateAfterHook",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "ValidateBeforeHook",
                table: "Modules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultApplyAfterHook",
                table: "Namespaces",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultApplyBeforeHook",
                table: "Namespaces",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DefaultAutoMigrateEnabled",
                table: "Namespaces",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DefaultAutoReconfigureEnabled",
                table: "Namespaces",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DefaultAutoUpgradeEnabled",
                table: "Namespaces",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultDestroyAfterHook",
                table: "Namespaces",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultDestroyBeforeHook",
                table: "Namespaces",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultInitAfterHook",
                table: "Namespaces",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultInitBeforeHook",
                table: "Namespaces",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultOutputAfterHook",
                table: "Namespaces",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultOutputBeforeHook",
                table: "Namespaces",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultPlanAfterHook",
                table: "Namespaces",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultPlanBeforeHook",
                table: "Namespaces",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultPlanDestroyAfterHook",
                table: "Namespaces",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultPlanDestroyBeforeHook",
                table: "Namespaces",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultValidateAfterHook",
                table: "Namespaces",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultValidateBeforeHook",
                table: "Namespaces",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplyAfterHook",
                table: "Modules",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplyBeforeHook",
                table: "Modules",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoMigrateEnabled",
                table: "Modules",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoReconfigureEnabled",
                table: "Modules",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoUpgradeEnabled",
                table: "Modules",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestroyAfterHook",
                table: "Modules",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestroyBeforeHook",
                table: "Modules",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreNamespaceBackendConfigs",
                table: "Modules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "InitAfterHook",
                table: "Modules",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InitBeforeHook",
                table: "Modules",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutputAfterHook",
                table: "Modules",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutputBeforeHook",
                table: "Modules",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanAfterHook",
                table: "Modules",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanBeforeHook",
                table: "Modules",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanDestroyAfterHook",
                table: "Modules",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanDestroyBeforeHook",
                table: "Modules",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidateAfterHook",
                table: "Modules",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidateBeforeHook",
                table: "Modules",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ModuleBackendConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleBackendConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleBackendConfigs_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleBackendConfigs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceBackendConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByPrincipalDiscriminator = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NamespaceBackendConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NamespaceBackendConfigs_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceBackendConfigs_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleBackendConfigs_ModuleId_Name",
                table: "ModuleBackendConfigs",
                columns: new[] { "ModuleId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleBackendConfigs_ModuleId_OrganizationId",
                table: "ModuleBackendConfigs",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleBackendConfigs_OrganizationId",
                table: "ModuleBackendConfigs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceBackendConfigs_NamespaceId_Name",
                table: "NamespaceBackendConfigs",
                columns: new[] { "NamespaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceBackendConfigs_NamespaceId_OrganizationId",
                table: "NamespaceBackendConfigs",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceBackendConfigs_OrganizationId",
                table: "NamespaceBackendConfigs",
                column: "OrganizationId");
        }
    }
}
