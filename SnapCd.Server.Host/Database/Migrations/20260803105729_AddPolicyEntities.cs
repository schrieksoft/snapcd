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
    public partial class AddPolicyEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModulePulumiInlinePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PolicyContent = table.Column<string>(type: "nvarchar(max)", maxLength: 65535, nullable: false),
                    Runtime = table.Column<int>(type: "int", nullable: false),
                    AdditionalDependencies = table.Column<string>(type: "nvarchar(max)", maxLength: 65535, nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EvaluateOn = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ModulePulumiInlinePolicies", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModulePulumiInlinePolicies_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModulePulumiInlinePolicies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModulePulumiLocalPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EvaluateOn = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ModulePulumiLocalPolicies", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModulePulumiLocalPolicies_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModulePulumiLocalPolicies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModulePulumiRemotePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RepoUrl = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    Revision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EvaluateOn = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ModulePulumiRemotePolicies", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModulePulumiRemotePolicies_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModulePulumiRemotePolicies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModuleTerraformInlinePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PolicyContent = table.Column<string>(type: "nvarchar(max)", maxLength: 65535, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EvaluateOn = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ModuleTerraformInlinePolicies", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleTerraformInlinePolicies_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleTerraformInlinePolicies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModuleTerraformLocalPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EvaluateOn = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ModuleTerraformLocalPolicies", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleTerraformLocalPolicies_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleTerraformLocalPolicies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModuleTerraformRemotePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RepoUrl = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    Revision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EvaluateOn = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ModuleTerraformRemotePolicies", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_ModuleTerraformRemotePolicies_Modules_ModuleId_OrganizationId",
                        columns: x => new { x.ModuleId, x.OrganizationId },
                        principalTable: "Modules",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleTerraformRemotePolicies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NamespacePulumiInlinePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PolicyContent = table.Column<string>(type: "nvarchar(max)", maxLength: 65535, nullable: false),
                    Runtime = table.Column<int>(type: "int", nullable: false),
                    AdditionalDependencies = table.Column<string>(type: "nvarchar(max)", maxLength: 65535, nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EvaluateOn = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_NamespacePulumiInlinePolicies", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_NamespacePulumiInlinePolicies_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespacePulumiInlinePolicies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NamespacePulumiLocalPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EvaluateOn = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_NamespacePulumiLocalPolicies", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_NamespacePulumiLocalPolicies_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespacePulumiLocalPolicies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NamespacePulumiRemotePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RepoUrl = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    Revision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EvaluateOn = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_NamespacePulumiRemotePolicies", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_NamespacePulumiRemotePolicies_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespacePulumiRemotePolicies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceTerraformInlinePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PolicyContent = table.Column<string>(type: "nvarchar(max)", maxLength: 65535, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EvaluateOn = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_NamespaceTerraformInlinePolicies", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_NamespaceTerraformInlinePolicies_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceTerraformInlinePolicies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceTerraformLocalPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EvaluateOn = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_NamespaceTerraformLocalPolicies", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_NamespaceTerraformLocalPolicies_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceTerraformLocalPolicies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NamespaceTerraformRemotePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NamespaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RepoUrl = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    Revision = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    EvaluateOn = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_NamespaceTerraformRemotePolicies", x => new { x.Id, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_NamespaceTerraformRemotePolicies_Namespaces_NamespaceId_OrganizationId",
                        columns: x => new { x.NamespaceId, x.OrganizationId },
                        principalTable: "Namespaces",
                        principalColumns: new[] { "Id", "OrganizationId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NamespaceTerraformRemotePolicies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiInlinePolicies_Id",
                table: "ModulePulumiInlinePolicies",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiInlinePolicies_ModuleId_Name",
                table: "ModulePulumiInlinePolicies",
                columns: new[] { "ModuleId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiInlinePolicies_ModuleId_OrganizationId",
                table: "ModulePulumiInlinePolicies",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiInlinePolicies_OrganizationId",
                table: "ModulePulumiInlinePolicies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiLocalPolicies_Id",
                table: "ModulePulumiLocalPolicies",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiLocalPolicies_ModuleId_Name",
                table: "ModulePulumiLocalPolicies",
                columns: new[] { "ModuleId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiLocalPolicies_ModuleId_OrganizationId",
                table: "ModulePulumiLocalPolicies",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiLocalPolicies_OrganizationId",
                table: "ModulePulumiLocalPolicies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiRemotePolicies_Id",
                table: "ModulePulumiRemotePolicies",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiRemotePolicies_ModuleId_Name",
                table: "ModulePulumiRemotePolicies",
                columns: new[] { "ModuleId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiRemotePolicies_ModuleId_OrganizationId",
                table: "ModulePulumiRemotePolicies",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModulePulumiRemotePolicies_OrganizationId",
                table: "ModulePulumiRemotePolicies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformInlinePolicies_Id",
                table: "ModuleTerraformInlinePolicies",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformInlinePolicies_ModuleId_Name",
                table: "ModuleTerraformInlinePolicies",
                columns: new[] { "ModuleId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformInlinePolicies_ModuleId_OrganizationId",
                table: "ModuleTerraformInlinePolicies",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformInlinePolicies_OrganizationId",
                table: "ModuleTerraformInlinePolicies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformLocalPolicies_Id",
                table: "ModuleTerraformLocalPolicies",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformLocalPolicies_ModuleId_Name",
                table: "ModuleTerraformLocalPolicies",
                columns: new[] { "ModuleId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformLocalPolicies_ModuleId_OrganizationId",
                table: "ModuleTerraformLocalPolicies",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformLocalPolicies_OrganizationId",
                table: "ModuleTerraformLocalPolicies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformRemotePolicies_Id",
                table: "ModuleTerraformRemotePolicies",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformRemotePolicies_ModuleId_Name",
                table: "ModuleTerraformRemotePolicies",
                columns: new[] { "ModuleId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformRemotePolicies_ModuleId_OrganizationId",
                table: "ModuleTerraformRemotePolicies",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleTerraformRemotePolicies_OrganizationId",
                table: "ModuleTerraformRemotePolicies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiInlinePolicies_Id",
                table: "NamespacePulumiInlinePolicies",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiInlinePolicies_NamespaceId_Name",
                table: "NamespacePulumiInlinePolicies",
                columns: new[] { "NamespaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiInlinePolicies_NamespaceId_OrganizationId",
                table: "NamespacePulumiInlinePolicies",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiInlinePolicies_OrganizationId",
                table: "NamespacePulumiInlinePolicies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiLocalPolicies_Id",
                table: "NamespacePulumiLocalPolicies",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiLocalPolicies_NamespaceId_Name",
                table: "NamespacePulumiLocalPolicies",
                columns: new[] { "NamespaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiLocalPolicies_NamespaceId_OrganizationId",
                table: "NamespacePulumiLocalPolicies",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiLocalPolicies_OrganizationId",
                table: "NamespacePulumiLocalPolicies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiRemotePolicies_Id",
                table: "NamespacePulumiRemotePolicies",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiRemotePolicies_NamespaceId_Name",
                table: "NamespacePulumiRemotePolicies",
                columns: new[] { "NamespaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiRemotePolicies_NamespaceId_OrganizationId",
                table: "NamespacePulumiRemotePolicies",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespacePulumiRemotePolicies_OrganizationId",
                table: "NamespacePulumiRemotePolicies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformInlinePolicies_Id",
                table: "NamespaceTerraformInlinePolicies",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformInlinePolicies_NamespaceId_Name",
                table: "NamespaceTerraformInlinePolicies",
                columns: new[] { "NamespaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformInlinePolicies_NamespaceId_OrganizationId",
                table: "NamespaceTerraformInlinePolicies",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformInlinePolicies_OrganizationId",
                table: "NamespaceTerraformInlinePolicies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformLocalPolicies_Id",
                table: "NamespaceTerraformLocalPolicies",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformLocalPolicies_NamespaceId_Name",
                table: "NamespaceTerraformLocalPolicies",
                columns: new[] { "NamespaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformLocalPolicies_NamespaceId_OrganizationId",
                table: "NamespaceTerraformLocalPolicies",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformLocalPolicies_OrganizationId",
                table: "NamespaceTerraformLocalPolicies",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformRemotePolicies_Id",
                table: "NamespaceTerraformRemotePolicies",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformRemotePolicies_NamespaceId_Name",
                table: "NamespaceTerraformRemotePolicies",
                columns: new[] { "NamespaceId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformRemotePolicies_NamespaceId_OrganizationId",
                table: "NamespaceTerraformRemotePolicies",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceTerraformRemotePolicies_OrganizationId",
                table: "NamespaceTerraformRemotePolicies",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModulePulumiInlinePolicies");

            migrationBuilder.DropTable(
                name: "ModulePulumiLocalPolicies");

            migrationBuilder.DropTable(
                name: "ModulePulumiRemotePolicies");

            migrationBuilder.DropTable(
                name: "ModuleTerraformInlinePolicies");

            migrationBuilder.DropTable(
                name: "ModuleTerraformLocalPolicies");

            migrationBuilder.DropTable(
                name: "ModuleTerraformRemotePolicies");

            migrationBuilder.DropTable(
                name: "NamespacePulumiInlinePolicies");

            migrationBuilder.DropTable(
                name: "NamespacePulumiLocalPolicies");

            migrationBuilder.DropTable(
                name: "NamespacePulumiRemotePolicies");

            migrationBuilder.DropTable(
                name: "NamespaceTerraformInlinePolicies");

            migrationBuilder.DropTable(
                name: "NamespaceTerraformLocalPolicies");

            migrationBuilder.DropTable(
                name: "NamespaceTerraformRemotePolicies");
        }
    }
}
