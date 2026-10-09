// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore.Migrations;
using SnapCd.Server.Host.Database.Migrations.Sql;

#nullable disable

namespace SnapCd.Server.Host.Database.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Triggers and procedures that maintain the four derived role assignment tables.
    ///
    /// IdentityAccessMetadataReader on the organization belongs to anyone who may grant a role
    /// somewhere, since choosing who to grant it to means seeing the organization's principals.
    /// MetadataReader on a Runner, Agent or Integration belongs to anyone who can read a scope it
    /// was supplied to.
    ///
    /// Both are derived on write: asking either at read time costs a seek per reachable group per
    /// scope, too much to pay per keystroke in a picker.
    /// </summary>
    public partial class DerivedRoleAssignmentDerivation : Migration
    {
        private const string SqlResource =
            "SnapCd.Server.Host.Database.Migrations.Sql.20261007174243_DerivedRoleAssignmentDerivation.sql";

        private static readonly string[] Procedures =
        [
            "dbo.usp_SyncIdentityAccessMetadataReaders",
            "dbo.usp_SyncDerivedRunnerMetadataReaders",
            "dbo.usp_SyncDerivedAgentMetadataReaders",
            "dbo.usp_SyncDerivedIntegrationMetadataReaders",
        ];

        private static readonly string[] Triggers =
        [
            "dbo.trg_OrganizationRoleAssignments_IamMetadataReader",
            "dbo.trg_StackRoleAssignments_IamMetadataReader",
            "dbo.trg_NamespaceRoleAssignments_IamMetadataReader",
            "dbo.trg_ModuleRoleAssignments_IamMetadataReader",
            "dbo.trg_RunnerRoleAssignments_IamMetadataReader",
            "dbo.trg_AgentRoleAssignments_IamMetadataReader",
            "dbo.trg_StateStoreRoleAssignments_IamMetadataReader",
            "dbo.trg_IntegrationRoleAssignments_IamMetadataReader",
            "dbo.trg_GroupMembers_IamMetadataReader",
            "dbo.trg_RunnerStackSupplies_SupplyMetadataReader",
            "dbo.trg_RunnerNamespaceSupplies_SupplyMetadataReader",
            "dbo.trg_RunnerModuleSupplies_SupplyMetadataReader",
            "dbo.trg_AgentStackSupplies_SupplyMetadataReader",
            "dbo.trg_AgentNamespaceSupplies_SupplyMetadataReader",
            "dbo.trg_AgentModuleSupplies_SupplyMetadataReader",
            "dbo.trg_IntegrationStackSupplies_SupplyMetadataReader",
            "dbo.trg_IntegrationNamespaceSupplies_SupplyMetadataReader",
            "dbo.trg_IntegrationModuleSupplies_SupplyMetadataReader",
            "dbo.trg_Runners_SupplyMetadataReader",
            "dbo.trg_Agents_SupplyMetadataReader",
            "dbo.trg_Integrations_SupplyMetadataReader",
            "dbo.trg_OrganizationRoleAssignments_SupplyMetadataReader",
            "dbo.trg_StackRoleAssignments_SupplyMetadataReader",
            "dbo.trg_NamespaceRoleAssignments_SupplyMetadataReader",
            "dbo.trg_ModuleRoleAssignments_SupplyMetadataReader",
            "dbo.trg_GroupMembers_SupplyMetadataReader",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var batch in MigrationSql.ReadBatches(SqlResource))
            {
                migrationBuilder.Sql(batch);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var trigger in Triggers)
            {
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {trigger};");
            }

            foreach (var procedure in Procedures)
            {
                migrationBuilder.Sql($"DROP PROCEDURE IF EXISTS {procedure};");
            }

            migrationBuilder.Sql("DROP TYPE IF EXISTS dbo.PrincipalScopeList;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS dbo.DerivedEntityList;");
            migrationBuilder.Sql("DROP TYPE IF EXISTS dbo.DerivedPrincipalList;");
        }
    }
}
