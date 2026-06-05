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
    public partial class Agents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "VaultSecrets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "VaultSecrets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "VariableSets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "VariableSets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "Variables",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "Variables",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "UserSystemRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "UserSystemRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "Stacks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "Stacks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "StackRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "StackRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "SourceRefresherPreselections",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "SourceRefresherPreselections",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "ServicePrincipalSystemRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "ServicePrincipalSystemRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "ServicePrincipals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "ServicePrincipals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "SelfHostedOrganizationLicenses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "SelfHostedOrganizationLicenses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "Secrets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "Secrets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "SecretMigrationAudits",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "SecretMigrationAudits",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "RunnerStackAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "RunnerStackAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "Runners",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "Runners",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "RunnerRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "RunnerRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "RunnerNamespaceAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "RunnerNamespaceAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "RunnerModuleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "RunnerModuleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "RunnerConnections",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "RunnerConnections",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "RunnerConnectionJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "RunnerConnectionJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "PreviewFeatureAcceptances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "PreviewFeatureAcceptances",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "OutputSets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "OutputSets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "Outputs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "Outputs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "OrganizationUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "OrganizationUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "Organizations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "Organizations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "OrganizationRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "OrganizationRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "NamespaceTerraformFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "NamespaceTerraformFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "NamespaceTerraformArrayFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "NamespaceTerraformArrayFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "Namespaces",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "Namespaces",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "NamespaceRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "NamespaceRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "NamespacePulumiFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "NamespacePulumiFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "NamespacePulumiArrayFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "NamespacePulumiArrayFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "NamespaceInputs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "NamespaceInputs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "NamespaceHooks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "NamespaceHooks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "NamespaceExtraFiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "NamespaceExtraFiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "ModuleTerraformFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "ModuleTerraformFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "ModuleTerraformArrayFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "ModuleTerraformArrayFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "Modules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "Modules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "ModuleRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "ModuleRoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "ModulePulumiFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "ModulePulumiFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "ModulePulumiArrayFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "ModulePulumiArrayFlags",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "ModuleJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "ModuleJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AgentId",
                table: "ModuleJobApprovals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "ModuleJobApprovals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "ModuleJobApprovals",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "ModuleJobApprovals",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "ModuleInputs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "ModuleInputs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "ModuleHooks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "ModuleHooks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "ModuleExtraFiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "ModuleExtraFiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "JobRunnerAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "JobRunnerAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "Groups",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "Groups",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "GroupMembers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "GroupMembers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByAgentId",
                table: "DependsOnModules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedByAgentId",
                table: "DependsOnModules",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServicePrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false),
                    AllowMultipleInstances = table.Column<bool>(type: "bit", nullable: false),
                    IsAssignedToAllModules = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Agents", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_Agents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Agents_ServicePrincipals_ServicePrincipalId_OrganizationId",
                        columns: x => new { x.ServicePrincipalId, x.OrganizationId },
                        principalTable: "ServicePrincipals",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModuleJobMissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MissionType = table.Column<int>(type: "int", nullable: false),
                    SidecarName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ResultSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_ModuleJobMissions", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleJobMissions_ModuleJobs_ModuleJobId_OrganizationId",
                        columns: x => new { x.ModuleJobId, x.OrganizationId },
                        principalTable: "ModuleJobs",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleJobMissions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstanceName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SignalRConnectionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ServerInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_AgentConnections", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_AgentConnections_Agents_AgentId_OrganizationId",
                        columns: x => new { x.AgentId, x.OrganizationId },
                        principalTable: "Agents",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentConnections_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentModuleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_AgentModuleAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_AgentModuleAssignments_Agents_AgentId_OrganizationId",
                        columns: x => new { x.AgentId, x.OrganizationId },
                        principalTable: "Agents",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentModuleAssignments_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentModuleAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentNamespaceAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_AgentNamespaceAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_AgentNamespaceAssignments_Agents_AgentId_OrganizationId",
                        columns: x => new { x.AgentId, x.OrganizationId },
                        principalTable: "Agents",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentNamespaceAssignments_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentNamespaceAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, computedColumnSql: "CASE WHEN [PrincipalDiscriminator] = 'User' THEN [UserId] WHEN [PrincipalDiscriminator] = 'ServicePrincipal' THEN [ServicePrincipalId] WHEN [PrincipalDiscriminator] = 'Group' THEN [GroupId] END", stored: true),
                    PrincipalDiscriminator = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServicePrincipalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_AgentRoleAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_AgentRoleAssignments_Agents_AgentId_OrganizationId",
                        columns: x => new { x.AgentId, x.OrganizationId },
                        principalTable: "Agents",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentRoleAssignments_Groups_GroupId_OrganizationId",
                        columns: x => new { x.GroupId, x.OrganizationId },
                        principalTable: "Groups",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentRoleAssignments_OrganizationUsers_UserId_OrganizationId",
                        columns: x => new { x.UserId, x.OrganizationId },
                        principalTable: "OrganizationUsers",
                        principalColumns: new[] { "UserId", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentRoleAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentRoleAssignments_ServicePrincipals_ServicePrincipalId_OrganizationId",
                        columns: x => new { x.ServicePrincipalId, x.OrganizationId },
                        principalTable: "ServicePrincipals",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentStackAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_AgentStackAssignments", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_AgentStackAssignments_Agents_AgentId_OrganizationId",
                        columns: x => new { x.AgentId, x.OrganizationId },
                        principalTable: "Agents",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentStackAssignments_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentStackAssignments_Stacks_StackId_OrganizationId",
                        columns: x => new { x.StackId, x.OrganizationId },
                        principalTable: "Stacks",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModuleMissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MissionType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SidecarName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
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
                    table.PrimaryKey("PK_ModuleMissions", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleMissions_Agents_AgentId_OrganizationId",
                        columns: x => new { x.AgentId, x.OrganizationId },
                        principalTable: "Agents",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleMissions_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleMissions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceMissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MissionType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SidecarName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
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
                    table.PrimaryKey("PK_NamespaceMissions", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_NamespaceMissions_Agents_AgentId_OrganizationId",
                        columns: x => new { x.AgentId, x.OrganizationId },
                        principalTable: "Agents",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceMissions_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceMissions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MissionType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SidecarName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
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
                    table.PrimaryKey("PK_OrganizationMissions", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_OrganizationMissions_Agents_AgentId_OrganizationId",
                        columns: x => new { x.AgentId, x.OrganizationId },
                        principalTable: "Agents",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationMissions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StackMissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MissionType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SidecarName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
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
                    table.PrimaryKey("PK_StackMissions", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_StackMissions_Agents_AgentId_OrganizationId",
                        columns: x => new { x.AgentId, x.OrganizationId },
                        principalTable: "Agents",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StackMissions_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StackMissions_Stacks_StackId_OrganizationId",
                        columns: x => new { x.StackId, x.OrganizationId },
                        principalTable: "Stacks",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModuleJobMissionRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleJobMissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MissionType = table.Column<int>(type: "int", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DeadlineAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastEventAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelRequestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AgentConnectionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServerInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SignalRConnectionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Logs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToolCallsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokensJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationSeconds = table.Column<double>(type: "float", nullable: true),
                    DiagnosisCategory = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
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
                    table.PrimaryKey("PK_ModuleJobMissionRuns", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleJobMissionRuns_ModuleJobMissions_ModuleJobMissionId_OrganizationId",
                        columns: x => new { x.ModuleJobMissionId, x.OrganizationId },
                        principalTable: "ModuleJobMissions",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleJobMissionRuns_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentConnections_AgentId_OrganizationId",
                table: "AgentConnections",
                columns: new[] { "AgentId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentConnections_Id",
                table: "AgentConnections",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentConnections_OrganizationId_AgentId_InstanceName",
                table: "AgentConnections",
                columns: new[] { "OrganizationId", "AgentId", "InstanceName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentConnections_OrganizationId_SignalRConnectionId",
                table: "AgentConnections",
                columns: new[] { "OrganizationId", "SignalRConnectionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentConnections_ServerInstanceId",
                table: "AgentConnections",
                column: "ServerInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentModuleAssignments_AgentId",
                table: "AgentModuleAssignments",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentModuleAssignments_AgentId_OrganizationId",
                table: "AgentModuleAssignments",
                columns: new[] { "AgentId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentModuleAssignments_Id",
                table: "AgentModuleAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentModuleAssignments_ModuleId",
                table: "AgentModuleAssignments",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentModuleAssignments_ModuleId_AgentId_OrganizationId",
                table: "AgentModuleAssignments",
                columns: new[] { "ModuleId", "AgentId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentModuleAssignments_ModuleId_OrganizationId",
                table: "AgentModuleAssignments",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentModuleAssignments_OrganizationId",
                table: "AgentModuleAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentNamespaceAssignments_AgentId",
                table: "AgentNamespaceAssignments",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentNamespaceAssignments_AgentId_OrganizationId",
                table: "AgentNamespaceAssignments",
                columns: new[] { "AgentId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentNamespaceAssignments_Id",
                table: "AgentNamespaceAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentNamespaceAssignments_NamespaceId",
                table: "AgentNamespaceAssignments",
                column: "NamespaceId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentNamespaceAssignments_NamespaceId_AgentId_OrganizationId",
                table: "AgentNamespaceAssignments",
                columns: new[] { "NamespaceId", "AgentId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentNamespaceAssignments_NamespaceId_OrganizationId",
                table: "AgentNamespaceAssignments",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentNamespaceAssignments_OrganizationId",
                table: "AgentNamespaceAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssignments_AgentId",
                table: "AgentRoleAssignments",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssignments_AgentId_OrganizationId",
                table: "AgentRoleAssignments",
                columns: new[] { "AgentId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssignments_GroupId",
                table: "AgentRoleAssignments",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssignments_GroupId_AgentId_OrganizationId_RoleName",
                table: "AgentRoleAssignments",
                columns: new[] { "GroupId", "AgentId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[GroupId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssignments_GroupId_OrganizationId",
                table: "AgentRoleAssignments",
                columns: new[] { "GroupId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssignments_Id",
                table: "AgentRoleAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssignments_OrganizationId",
                table: "AgentRoleAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssignments_PrincipalId",
                table: "AgentRoleAssignments",
                column: "PrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssignments_ServicePrincipalId",
                table: "AgentRoleAssignments",
                column: "ServicePrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssignments_ServicePrincipalId_AgentId_OrganizationId_RoleName",
                table: "AgentRoleAssignments",
                columns: new[] { "ServicePrincipalId", "AgentId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[ServicePrincipalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssignments_ServicePrincipalId_OrganizationId",
                table: "AgentRoleAssignments",
                columns: new[] { "ServicePrincipalId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssignments_UserId",
                table: "AgentRoleAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssignments_UserId_AgentId_OrganizationId_RoleName",
                table: "AgentRoleAssignments",
                columns: new[] { "UserId", "AgentId", "OrganizationId", "RoleName" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssignments_UserId_OrganizationId",
                table: "AgentRoleAssignments",
                columns: new[] { "UserId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Id",
                table: "Agents",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_Name_OrganizationId",
                table: "Agents",
                columns: new[] { "Name", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Agents_OrganizationId",
                table: "Agents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_ServicePrincipalId",
                table: "Agents",
                column: "ServicePrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_Agents_ServicePrincipalId_OrganizationId",
                table: "Agents",
                columns: new[] { "ServicePrincipalId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentStackAssignments_AgentId",
                table: "AgentStackAssignments",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentStackAssignments_AgentId_OrganizationId",
                table: "AgentStackAssignments",
                columns: new[] { "AgentId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentStackAssignments_Id",
                table: "AgentStackAssignments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentStackAssignments_OrganizationId",
                table: "AgentStackAssignments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentStackAssignments_StackId",
                table: "AgentStackAssignments",
                column: "StackId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentStackAssignments_StackId_AgentId_OrganizationId",
                table: "AgentStackAssignments",
                columns: new[] { "StackId", "AgentId", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentStackAssignments_StackId_OrganizationId",
                table: "AgentStackAssignments",
                columns: new[] { "StackId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissionRuns_AgentId",
                table: "ModuleJobMissionRuns",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissionRuns_Id",
                table: "ModuleJobMissionRuns",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissionRuns_InvocationId",
                table: "ModuleJobMissionRuns",
                column: "InvocationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissionRuns_ModuleJobId",
                table: "ModuleJobMissionRuns",
                column: "ModuleJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissionRuns_ModuleJobId_MissionType_OrganizationId",
                table: "ModuleJobMissionRuns",
                columns: new[] { "ModuleJobId", "MissionType", "OrganizationId" },
                unique: true,
                filter: "[Status] IN ('Pending', 'Running', 'AwaitingReconnect')");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissionRuns_ModuleJobMissionId",
                table: "ModuleJobMissionRuns",
                column: "ModuleJobMissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissionRuns_ModuleJobMissionId_OrganizationId",
                table: "ModuleJobMissionRuns",
                columns: new[] { "ModuleJobMissionId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissionRuns_OrganizationId",
                table: "ModuleJobMissionRuns",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissionRuns_Status",
                table: "ModuleJobMissionRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissions_AgentId",
                table: "ModuleJobMissions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissions_Id",
                table: "ModuleJobMissions",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissions_MissionId",
                table: "ModuleJobMissions",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissions_ModuleJobId",
                table: "ModuleJobMissions",
                column: "ModuleJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissions_ModuleJobId_MissionType_OrganizationId",
                table: "ModuleJobMissions",
                columns: new[] { "ModuleJobId", "MissionType", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissions_ModuleJobId_OrganizationId",
                table: "ModuleJobMissions",
                columns: new[] { "ModuleJobId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleJobMissions_OrganizationId",
                table: "ModuleJobMissions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleMissions_AgentId",
                table: "ModuleMissions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleMissions_AgentId_OrganizationId",
                table: "ModuleMissions",
                columns: new[] { "AgentId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleMissions_Id",
                table: "ModuleMissions",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleMissions_ModuleId",
                table: "ModuleMissions",
                column: "ModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleMissions_ModuleId_AgentId_OrganizationId_MissionType",
                table: "ModuleMissions",
                columns: new[] { "ModuleId", "AgentId", "OrganizationId", "MissionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleMissions_ModuleId_OrganizationId",
                table: "ModuleMissions",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleMissions_OrganizationId",
                table: "ModuleMissions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceMissions_AgentId",
                table: "NamespaceMissions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceMissions_AgentId_OrganizationId",
                table: "NamespaceMissions",
                columns: new[] { "AgentId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceMissions_Id",
                table: "NamespaceMissions",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceMissions_NamespaceId",
                table: "NamespaceMissions",
                column: "NamespaceId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceMissions_NamespaceId_AgentId_OrganizationId_MissionType",
                table: "NamespaceMissions",
                columns: new[] { "NamespaceId", "AgentId", "OrganizationId", "MissionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceMissions_NamespaceId_OrganizationId",
                table: "NamespaceMissions",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceMissions_OrganizationId",
                table: "NamespaceMissions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMissions_AgentId",
                table: "OrganizationMissions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMissions_AgentId_OrganizationId_MissionType",
                table: "OrganizationMissions",
                columns: new[] { "AgentId", "OrganizationId", "MissionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMissions_Id",
                table: "OrganizationMissions",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMissions_OrganizationId",
                table: "OrganizationMissions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StackMissions_AgentId",
                table: "StackMissions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_StackMissions_AgentId_OrganizationId",
                table: "StackMissions",
                columns: new[] { "AgentId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_StackMissions_Id",
                table: "StackMissions",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StackMissions_OrganizationId",
                table: "StackMissions",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_StackMissions_StackId",
                table: "StackMissions",
                column: "StackId");

            migrationBuilder.CreateIndex(
                name: "IX_StackMissions_StackId_AgentId_OrganizationId_MissionType",
                table: "StackMissions",
                columns: new[] { "StackId", "AgentId", "OrganizationId", "MissionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StackMissions_StackId_OrganizationId",
                table: "StackMissions",
                columns: new[] { "StackId", "OrganizationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentConnections");

            migrationBuilder.DropTable(
                name: "AgentModuleAssignments");

            migrationBuilder.DropTable(
                name: "AgentNamespaceAssignments");

            migrationBuilder.DropTable(
                name: "AgentRoleAssignments");

            migrationBuilder.DropTable(
                name: "AgentStackAssignments");

            migrationBuilder.DropTable(
                name: "ModuleJobMissionRuns");

            migrationBuilder.DropTable(
                name: "ModuleMissions");

            migrationBuilder.DropTable(
                name: "NamespaceMissions");

            migrationBuilder.DropTable(
                name: "OrganizationMissions");

            migrationBuilder.DropTable(
                name: "StackMissions");

            migrationBuilder.DropTable(
                name: "ModuleJobMissions");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "VaultSecrets");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "VaultSecrets");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "VariableSets");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "VariableSets");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "UserSystemRoleAssignments");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "UserSystemRoleAssignments");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "Stacks");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "Stacks");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "StackRoleAssignments");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "StackRoleAssignments");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "SourceRefresherPreselections");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "SourceRefresherPreselections");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "ServicePrincipalSystemRoleAssignments");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "ServicePrincipalSystemRoleAssignments");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "ServicePrincipals");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "ServicePrincipals");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "SelfHostedOrganizationLicenses");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "SelfHostedOrganizationLicenses");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "Secrets");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "Secrets");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "SecretMigrationAudits");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "SecretMigrationAudits");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "RunnerStackAssignments");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "RunnerStackAssignments");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "Runners");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "Runners");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "RunnerRoleAssignments");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "RunnerRoleAssignments");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "RunnerNamespaceAssignments");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "RunnerNamespaceAssignments");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "RunnerModuleAssignments");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "RunnerModuleAssignments");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "RunnerConnections");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "RunnerConnections");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "RunnerConnectionJobs");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "RunnerConnectionJobs");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "PreviewFeatureAcceptances");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "PreviewFeatureAcceptances");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "OutputSets");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "OutputSets");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "Outputs");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "Outputs");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "OrganizationUsers");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "OrganizationUsers");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "OrganizationRoleAssignments");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "OrganizationRoleAssignments");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "NamespaceTerraformFlags");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "NamespaceTerraformFlags");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "NamespaceTerraformArrayFlags");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "NamespaceTerraformArrayFlags");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "Namespaces");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "NamespaceRoleAssignments");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "NamespaceRoleAssignments");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "NamespacePulumiFlags");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "NamespacePulumiFlags");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "NamespacePulumiArrayFlags");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "NamespacePulumiArrayFlags");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "NamespaceInputs");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "NamespaceInputs");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "NamespaceHooks");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "NamespaceHooks");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "NamespaceExtraFiles");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "NamespaceExtraFiles");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "ModuleTerraformFlags");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "ModuleTerraformFlags");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "ModuleTerraformArrayFlags");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "ModuleTerraformArrayFlags");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "ModuleRoleAssignments");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "ModuleRoleAssignments");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "ModulePulumiFlags");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "ModulePulumiFlags");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "ModulePulumiArrayFlags");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "ModulePulumiArrayFlags");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "ModuleJobs");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "ModuleJobs");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "ModuleJobApprovals");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "ModuleJobApprovals");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "ModuleJobApprovals");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "ModuleJobApprovals");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "ModuleInputs");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "ModuleInputs");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "ModuleHooks");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "ModuleHooks");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "ModuleExtraFiles");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "ModuleExtraFiles");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "JobRunnerAssignments");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "JobRunnerAssignments");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "CreatedByAgentId",
                table: "DependsOnModules");

            migrationBuilder.DropColumn(
                name: "ModifiedByAgentId",
                table: "DependsOnModules");
        }
    }
}
