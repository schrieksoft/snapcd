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
    public partial class RenameAssignmentsToSupplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(name: "IX_AgentModuleAssignments_AgentId", newName: "IX_AgentModuleSupplies_AgentId", table: "AgentModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentModuleAssignments_Id", newName: "IX_AgentModuleSupplies_Id", table: "AgentModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentModuleAssignments_ModuleId", newName: "IX_AgentModuleSupplies_ModuleId", table: "AgentModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentModuleAssignments_OrganizationId", newName: "IX_AgentModuleSupplies_OrganizationId", table: "AgentModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentModuleAssignments_AgentId_OrganizationId", newName: "IX_AgentModuleSupplies_AgentId_OrganizationId", table: "AgentModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentModuleAssignments_ModuleId_OrganizationId", newName: "IX_AgentModuleSupplies_ModuleId_OrganizationId", table: "AgentModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentModuleAssignments_ModuleId_AgentId_OrganizationId", newName: "IX_AgentModuleSupplies_ModuleId_AgentId_OrganizationId", table: "AgentModuleAssignments");
            migrationBuilder.RenameTable(name: "AgentModuleAssignments", newName: "AgentModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentNamespaceAssignments_AgentId", newName: "IX_AgentNamespaceSupplies_AgentId", table: "AgentNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentNamespaceAssignments_Id", newName: "IX_AgentNamespaceSupplies_Id", table: "AgentNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentNamespaceAssignments_NamespaceId", newName: "IX_AgentNamespaceSupplies_NamespaceId", table: "AgentNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentNamespaceAssignments_OrganizationId", newName: "IX_AgentNamespaceSupplies_OrganizationId", table: "AgentNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentNamespaceAssignments_AgentId_OrganizationId", newName: "IX_AgentNamespaceSupplies_AgentId_OrganizationId", table: "AgentNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentNamespaceAssignments_NamespaceId_OrganizationId", newName: "IX_AgentNamespaceSupplies_NamespaceId_OrganizationId", table: "AgentNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentNamespaceAssignments_NamespaceId_AgentId_OrganizationId", newName: "IX_AgentNamespaceSupplies_NamespaceId_AgentId_OrganizationId", table: "AgentNamespaceAssignments");
            migrationBuilder.RenameTable(name: "AgentNamespaceAssignments", newName: "AgentNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentStackAssignments_AgentId", newName: "IX_AgentStackSupplies_AgentId", table: "AgentStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentStackAssignments_Id", newName: "IX_AgentStackSupplies_Id", table: "AgentStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentStackAssignments_OrganizationId", newName: "IX_AgentStackSupplies_OrganizationId", table: "AgentStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentStackAssignments_StackId", newName: "IX_AgentStackSupplies_StackId", table: "AgentStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentStackAssignments_AgentId_OrganizationId", newName: "IX_AgentStackSupplies_AgentId_OrganizationId", table: "AgentStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentStackAssignments_StackId_OrganizationId", newName: "IX_AgentStackSupplies_StackId_OrganizationId", table: "AgentStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentStackAssignments_StackId_AgentId_OrganizationId", newName: "IX_AgentStackSupplies_StackId_AgentId_OrganizationId", table: "AgentStackAssignments");
            migrationBuilder.RenameTable(name: "AgentStackAssignments", newName: "AgentStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationModuleAssignments_Id", newName: "IX_IntegrationModuleSupplies_Id", table: "IntegrationModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationModuleAssignments_IntegrationId", newName: "IX_IntegrationModuleSupplies_IntegrationId", table: "IntegrationModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationModuleAssignments_ModuleId", newName: "IX_IntegrationModuleSupplies_ModuleId", table: "IntegrationModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationModuleAssignments_OrganizationId", newName: "IX_IntegrationModuleSupplies_OrganizationId", table: "IntegrationModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationModuleAssignments_IntegrationId_OrganizationId", newName: "IX_IntegrationModuleSupplies_IntegrationId_OrganizationId", table: "IntegrationModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationModuleAssignments_ModuleId_OrganizationId", newName: "IX_IntegrationModuleSupplies_ModuleId_OrganizationId", table: "IntegrationModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationModuleAssignments_ModuleId_IntegrationId_OrganizationId", newName: "IX_IntegrationModuleSupplies_ModuleId_IntegrationId_OrganizationId", table: "IntegrationModuleAssignments");
            migrationBuilder.RenameTable(name: "IntegrationModuleAssignments", newName: "IntegrationModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationNamespaceAssignments_Id", newName: "IX_IntegrationNamespaceSupplies_Id", table: "IntegrationNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationNamespaceAssignments_IntegrationId", newName: "IX_IntegrationNamespaceSupplies_IntegrationId", table: "IntegrationNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationNamespaceAssignments_NamespaceId", newName: "IX_IntegrationNamespaceSupplies_NamespaceId", table: "IntegrationNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationNamespaceAssignments_OrganizationId", newName: "IX_IntegrationNamespaceSupplies_OrganizationId", table: "IntegrationNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationNamespaceAssignments_IntegrationId_OrganizationId", newName: "IX_IntegrationNamespaceSupplies_IntegrationId_OrganizationId", table: "IntegrationNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationNamespaceAssignments_NamespaceId_OrganizationId", newName: "IX_IntegrationNamespaceSupplies_NamespaceId_OrganizationId", table: "IntegrationNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationNamespaceAssignments_NamespaceId_IntegrationId_OrganizationId", newName: "IX_IntegrationNamespaceSupplies_NamespaceId_IntegrationId_OrganizationId", table: "IntegrationNamespaceAssignments");
            migrationBuilder.RenameTable(name: "IntegrationNamespaceAssignments", newName: "IntegrationNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationStackAssignments_Id", newName: "IX_IntegrationStackSupplies_Id", table: "IntegrationStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationStackAssignments_IntegrationId", newName: "IX_IntegrationStackSupplies_IntegrationId", table: "IntegrationStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationStackAssignments_OrganizationId", newName: "IX_IntegrationStackSupplies_OrganizationId", table: "IntegrationStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationStackAssignments_StackId", newName: "IX_IntegrationStackSupplies_StackId", table: "IntegrationStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationStackAssignments_IntegrationId_OrganizationId", newName: "IX_IntegrationStackSupplies_IntegrationId_OrganizationId", table: "IntegrationStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationStackAssignments_StackId_OrganizationId", newName: "IX_IntegrationStackSupplies_StackId_OrganizationId", table: "IntegrationStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationStackAssignments_StackId_IntegrationId_OrganizationId", newName: "IX_IntegrationStackSupplies_StackId_IntegrationId_OrganizationId", table: "IntegrationStackAssignments");
            migrationBuilder.RenameTable(name: "IntegrationStackAssignments", newName: "IntegrationStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerModuleAssignments_Id", newName: "IX_RunnerModuleSupplies_Id", table: "RunnerModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerModuleAssignments_ModuleId", newName: "IX_RunnerModuleSupplies_ModuleId", table: "RunnerModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerModuleAssignments_OrganizationId", newName: "IX_RunnerModuleSupplies_OrganizationId", table: "RunnerModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerModuleAssignments_RunnerId", newName: "IX_RunnerModuleSupplies_RunnerId", table: "RunnerModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerModuleAssignments_ModuleId_OrganizationId", newName: "IX_RunnerModuleSupplies_ModuleId_OrganizationId", table: "RunnerModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerModuleAssignments_RunnerId_OrganizationId", newName: "IX_RunnerModuleSupplies_RunnerId_OrganizationId", table: "RunnerModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerModuleAssignments_ModuleId_RunnerId_OrganizationId", newName: "IX_RunnerModuleSupplies_ModuleId_RunnerId_OrganizationId", table: "RunnerModuleAssignments");
            migrationBuilder.RenameTable(name: "RunnerModuleAssignments", newName: "RunnerModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerNamespaceAssignments_Id", newName: "IX_RunnerNamespaceSupplies_Id", table: "RunnerNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerNamespaceAssignments_NamespaceId", newName: "IX_RunnerNamespaceSupplies_NamespaceId", table: "RunnerNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerNamespaceAssignments_OrganizationId", newName: "IX_RunnerNamespaceSupplies_OrganizationId", table: "RunnerNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerNamespaceAssignments_RunnerId", newName: "IX_RunnerNamespaceSupplies_RunnerId", table: "RunnerNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerNamespaceAssignments_NamespaceId_OrganizationId", newName: "IX_RunnerNamespaceSupplies_NamespaceId_OrganizationId", table: "RunnerNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerNamespaceAssignments_RunnerId_OrganizationId", newName: "IX_RunnerNamespaceSupplies_RunnerId_OrganizationId", table: "RunnerNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerNamespaceAssignments_NamespaceId_RunnerId_OrganizationId", newName: "IX_RunnerNamespaceSupplies_NamespaceId_RunnerId_OrganizationId", table: "RunnerNamespaceAssignments");
            migrationBuilder.RenameTable(name: "RunnerNamespaceAssignments", newName: "RunnerNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerStackAssignments_Id", newName: "IX_RunnerStackSupplies_Id", table: "RunnerStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerStackAssignments_OrganizationId", newName: "IX_RunnerStackSupplies_OrganizationId", table: "RunnerStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerStackAssignments_RunnerId", newName: "IX_RunnerStackSupplies_RunnerId", table: "RunnerStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerStackAssignments_StackId", newName: "IX_RunnerStackSupplies_StackId", table: "RunnerStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerStackAssignments_RunnerId_OrganizationId", newName: "IX_RunnerStackSupplies_RunnerId_OrganizationId", table: "RunnerStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerStackAssignments_StackId_OrganizationId", newName: "IX_RunnerStackSupplies_StackId_OrganizationId", table: "RunnerStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerStackAssignments_StackId_RunnerId_OrganizationId", newName: "IX_RunnerStackSupplies_StackId_RunnerId_OrganizationId", table: "RunnerStackAssignments");
            migrationBuilder.RenameTable(name: "RunnerStackAssignments", newName: "RunnerStackSupplies");
            migrationBuilder.RenameColumn(name: "IsAssignedToAllModules", table: "Agents", newName: "IsSuppliedToAllModules");
            migrationBuilder.RenameColumn(name: "IsAssignedToAllModules", table: "Integrations", newName: "IsSuppliedToAllModules");
            migrationBuilder.RenameColumn(name: "IsAssignedToAllModules", table: "Runners", newName: "IsSuppliedToAllModules");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(name: "IX_AgentModuleSupplies_AgentId", newName: "IX_AgentModuleAssignments_AgentId", table: "AgentModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentModuleSupplies_Id", newName: "IX_AgentModuleAssignments_Id", table: "AgentModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentModuleSupplies_ModuleId", newName: "IX_AgentModuleAssignments_ModuleId", table: "AgentModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentModuleSupplies_OrganizationId", newName: "IX_AgentModuleAssignments_OrganizationId", table: "AgentModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentModuleSupplies_AgentId_OrganizationId", newName: "IX_AgentModuleAssignments_AgentId_OrganizationId", table: "AgentModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentModuleSupplies_ModuleId_OrganizationId", newName: "IX_AgentModuleAssignments_ModuleId_OrganizationId", table: "AgentModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentModuleSupplies_ModuleId_AgentId_OrganizationId", newName: "IX_AgentModuleAssignments_ModuleId_AgentId_OrganizationId", table: "AgentModuleSupplies");
            migrationBuilder.RenameTable(name: "AgentModuleSupplies", newName: "AgentModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentNamespaceSupplies_AgentId", newName: "IX_AgentNamespaceAssignments_AgentId", table: "AgentNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentNamespaceSupplies_Id", newName: "IX_AgentNamespaceAssignments_Id", table: "AgentNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentNamespaceSupplies_NamespaceId", newName: "IX_AgentNamespaceAssignments_NamespaceId", table: "AgentNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentNamespaceSupplies_OrganizationId", newName: "IX_AgentNamespaceAssignments_OrganizationId", table: "AgentNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentNamespaceSupplies_AgentId_OrganizationId", newName: "IX_AgentNamespaceAssignments_AgentId_OrganizationId", table: "AgentNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentNamespaceSupplies_NamespaceId_OrganizationId", newName: "IX_AgentNamespaceAssignments_NamespaceId_OrganizationId", table: "AgentNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentNamespaceSupplies_NamespaceId_AgentId_OrganizationId", newName: "IX_AgentNamespaceAssignments_NamespaceId_AgentId_OrganizationId", table: "AgentNamespaceSupplies");
            migrationBuilder.RenameTable(name: "AgentNamespaceSupplies", newName: "AgentNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_AgentStackSupplies_AgentId", newName: "IX_AgentStackAssignments_AgentId", table: "AgentStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentStackSupplies_Id", newName: "IX_AgentStackAssignments_Id", table: "AgentStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentStackSupplies_OrganizationId", newName: "IX_AgentStackAssignments_OrganizationId", table: "AgentStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentStackSupplies_StackId", newName: "IX_AgentStackAssignments_StackId", table: "AgentStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentStackSupplies_AgentId_OrganizationId", newName: "IX_AgentStackAssignments_AgentId_OrganizationId", table: "AgentStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentStackSupplies_StackId_OrganizationId", newName: "IX_AgentStackAssignments_StackId_OrganizationId", table: "AgentStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_AgentStackSupplies_StackId_AgentId_OrganizationId", newName: "IX_AgentStackAssignments_StackId_AgentId_OrganizationId", table: "AgentStackSupplies");
            migrationBuilder.RenameTable(name: "AgentStackSupplies", newName: "AgentStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationModuleSupplies_Id", newName: "IX_IntegrationModuleAssignments_Id", table: "IntegrationModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationModuleSupplies_IntegrationId", newName: "IX_IntegrationModuleAssignments_IntegrationId", table: "IntegrationModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationModuleSupplies_ModuleId", newName: "IX_IntegrationModuleAssignments_ModuleId", table: "IntegrationModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationModuleSupplies_OrganizationId", newName: "IX_IntegrationModuleAssignments_OrganizationId", table: "IntegrationModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationModuleSupplies_IntegrationId_OrganizationId", newName: "IX_IntegrationModuleAssignments_IntegrationId_OrganizationId", table: "IntegrationModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationModuleSupplies_ModuleId_OrganizationId", newName: "IX_IntegrationModuleAssignments_ModuleId_OrganizationId", table: "IntegrationModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationModuleSupplies_ModuleId_IntegrationId_OrganizationId", newName: "IX_IntegrationModuleAssignments_ModuleId_IntegrationId_OrganizationId", table: "IntegrationModuleSupplies");
            migrationBuilder.RenameTable(name: "IntegrationModuleSupplies", newName: "IntegrationModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationNamespaceSupplies_Id", newName: "IX_IntegrationNamespaceAssignments_Id", table: "IntegrationNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationNamespaceSupplies_IntegrationId", newName: "IX_IntegrationNamespaceAssignments_IntegrationId", table: "IntegrationNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationNamespaceSupplies_NamespaceId", newName: "IX_IntegrationNamespaceAssignments_NamespaceId", table: "IntegrationNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationNamespaceSupplies_OrganizationId", newName: "IX_IntegrationNamespaceAssignments_OrganizationId", table: "IntegrationNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationNamespaceSupplies_IntegrationId_OrganizationId", newName: "IX_IntegrationNamespaceAssignments_IntegrationId_OrganizationId", table: "IntegrationNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationNamespaceSupplies_NamespaceId_OrganizationId", newName: "IX_IntegrationNamespaceAssignments_NamespaceId_OrganizationId", table: "IntegrationNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationNamespaceSupplies_NamespaceId_IntegrationId_OrganizationId", newName: "IX_IntegrationNamespaceAssignments_NamespaceId_IntegrationId_OrganizationId", table: "IntegrationNamespaceSupplies");
            migrationBuilder.RenameTable(name: "IntegrationNamespaceSupplies", newName: "IntegrationNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_IntegrationStackSupplies_Id", newName: "IX_IntegrationStackAssignments_Id", table: "IntegrationStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationStackSupplies_IntegrationId", newName: "IX_IntegrationStackAssignments_IntegrationId", table: "IntegrationStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationStackSupplies_OrganizationId", newName: "IX_IntegrationStackAssignments_OrganizationId", table: "IntegrationStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationStackSupplies_StackId", newName: "IX_IntegrationStackAssignments_StackId", table: "IntegrationStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationStackSupplies_IntegrationId_OrganizationId", newName: "IX_IntegrationStackAssignments_IntegrationId_OrganizationId", table: "IntegrationStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationStackSupplies_StackId_OrganizationId", newName: "IX_IntegrationStackAssignments_StackId_OrganizationId", table: "IntegrationStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_IntegrationStackSupplies_StackId_IntegrationId_OrganizationId", newName: "IX_IntegrationStackAssignments_StackId_IntegrationId_OrganizationId", table: "IntegrationStackSupplies");
            migrationBuilder.RenameTable(name: "IntegrationStackSupplies", newName: "IntegrationStackAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerModuleSupplies_Id", newName: "IX_RunnerModuleAssignments_Id", table: "RunnerModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerModuleSupplies_ModuleId", newName: "IX_RunnerModuleAssignments_ModuleId", table: "RunnerModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerModuleSupplies_OrganizationId", newName: "IX_RunnerModuleAssignments_OrganizationId", table: "RunnerModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerModuleSupplies_RunnerId", newName: "IX_RunnerModuleAssignments_RunnerId", table: "RunnerModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerModuleSupplies_ModuleId_OrganizationId", newName: "IX_RunnerModuleAssignments_ModuleId_OrganizationId", table: "RunnerModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerModuleSupplies_RunnerId_OrganizationId", newName: "IX_RunnerModuleAssignments_RunnerId_OrganizationId", table: "RunnerModuleSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerModuleSupplies_ModuleId_RunnerId_OrganizationId", newName: "IX_RunnerModuleAssignments_ModuleId_RunnerId_OrganizationId", table: "RunnerModuleSupplies");
            migrationBuilder.RenameTable(name: "RunnerModuleSupplies", newName: "RunnerModuleAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerNamespaceSupplies_Id", newName: "IX_RunnerNamespaceAssignments_Id", table: "RunnerNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerNamespaceSupplies_NamespaceId", newName: "IX_RunnerNamespaceAssignments_NamespaceId", table: "RunnerNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerNamespaceSupplies_OrganizationId", newName: "IX_RunnerNamespaceAssignments_OrganizationId", table: "RunnerNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerNamespaceSupplies_RunnerId", newName: "IX_RunnerNamespaceAssignments_RunnerId", table: "RunnerNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerNamespaceSupplies_NamespaceId_OrganizationId", newName: "IX_RunnerNamespaceAssignments_NamespaceId_OrganizationId", table: "RunnerNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerNamespaceSupplies_RunnerId_OrganizationId", newName: "IX_RunnerNamespaceAssignments_RunnerId_OrganizationId", table: "RunnerNamespaceSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerNamespaceSupplies_NamespaceId_RunnerId_OrganizationId", newName: "IX_RunnerNamespaceAssignments_NamespaceId_RunnerId_OrganizationId", table: "RunnerNamespaceSupplies");
            migrationBuilder.RenameTable(name: "RunnerNamespaceSupplies", newName: "RunnerNamespaceAssignments");
            migrationBuilder.RenameIndex(name: "IX_RunnerStackSupplies_Id", newName: "IX_RunnerStackAssignments_Id", table: "RunnerStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerStackSupplies_OrganizationId", newName: "IX_RunnerStackAssignments_OrganizationId", table: "RunnerStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerStackSupplies_RunnerId", newName: "IX_RunnerStackAssignments_RunnerId", table: "RunnerStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerStackSupplies_StackId", newName: "IX_RunnerStackAssignments_StackId", table: "RunnerStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerStackSupplies_RunnerId_OrganizationId", newName: "IX_RunnerStackAssignments_RunnerId_OrganizationId", table: "RunnerStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerStackSupplies_StackId_OrganizationId", newName: "IX_RunnerStackAssignments_StackId_OrganizationId", table: "RunnerStackSupplies");
            migrationBuilder.RenameIndex(name: "IX_RunnerStackSupplies_StackId_RunnerId_OrganizationId", newName: "IX_RunnerStackAssignments_StackId_RunnerId_OrganizationId", table: "RunnerStackSupplies");
            migrationBuilder.RenameTable(name: "RunnerStackSupplies", newName: "RunnerStackAssignments");
            migrationBuilder.RenameColumn(name: "IsSuppliedToAllModules", table: "Agents", newName: "IsAssignedToAllModules");
            migrationBuilder.RenameColumn(name: "IsSuppliedToAllModules", table: "Integrations", newName: "IsAssignedToAllModules");
            migrationBuilder.RenameColumn(name: "IsSuppliedToAllModules", table: "Runners", newName: "IsAssignedToAllModules");
        }
    }
}

