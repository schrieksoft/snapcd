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
    public partial class PermissionQueryOptimizationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StackRoleAssignments_StackId_OrganizationId",
                table: "StackRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RunnerRoleAssignments_RunnerId_OrganizationId",
                table: "RunnerRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_NamespaceRoleAssignments_NamespaceId_OrganizationId",
                table: "NamespaceRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ModuleRoleAssignments_ModuleId_OrganizationId",
                table: "ModuleRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_IntegrationRoleAssignments_IntegrationId_OrganizationId",
                table: "IntegrationRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AgentRoleAssignments_AgentId_OrganizationId",
                table: "AgentRoleAssignments");

            // Handle the RecursiveGroupMembers table. On existing databases it may already
            // exist as RecursiveGroupMembersPhysical (from the old idempotent SQL script).
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RecursiveGroupMembersPhysical')
                BEGIN
                    IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_RecursiveGroupMember')
                        DROP VIEW dbo.vw_RecursiveGroupMember;

                    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('RecursiveGroupMembersPhysical') AND name = 'VisitedPath')
                        ALTER TABLE dbo.RecursiveGroupMembersPhysical DROP COLUMN VisitedPath;

                    EXEC sp_rename 'dbo.RecursiveGroupMembersPhysical', 'RecursiveGroupMembers';

                    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_RecursiveGroupMembersPhysical')
                        EXEC sp_rename 'PK_RecursiveGroupMembersPhysical', 'PK_RecursiveGroupMembers', 'OBJECT';

                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RGMP_RootGroup')
                        EXEC sp_rename 'FK_RGMP_RootGroup', 'FK_RecursiveGroupMembers_Groups_RootGroupId_RootOrganizationId', 'OBJECT';
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RGMP_Group')
                        EXEC sp_rename 'FK_RGMP_Group', 'FK_RecursiveGroupMembers_Groups_GroupId_OrganizationId', 'OBJECT';

                    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_RecursiveGroupMembers_Organizations_OrganizationId')
                        ALTER TABLE dbo.RecursiveGroupMembers
                            ADD CONSTRAINT FK_RecursiveGroupMembers_Organizations_OrganizationId
                            FOREIGN KEY (OrganizationId) REFERENCES dbo.Organizations(Id);

                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecursiveGroupMembers_OrganizationId' AND object_id = OBJECT_ID('RecursiveGroupMembers'))
                        CREATE NONCLUSTERED INDEX IX_RecursiveGroupMembers_OrganizationId
                            ON dbo.RecursiveGroupMembers (OrganizationId);
                END
                ELSE IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RecursiveGroupMembers')
                BEGIN
                    CREATE TABLE dbo.RecursiveGroupMembers (
                        RootGroupId        uniqueidentifier NOT NULL,
                        RootOrganizationId uniqueidentifier NOT NULL,
                        GroupId            uniqueidentifier NOT NULL,
                        OrganizationId     uniqueidentifier NOT NULL,
                        RootGroupName      nvarchar(max)    NOT NULL,
                        GroupName          nvarchar(max)    NOT NULL,
                        Depth              int              NOT NULL,

                        CONSTRAINT PK_RecursiveGroupMembers
                            PRIMARY KEY CLUSTERED (RootGroupId, RootOrganizationId, GroupId, OrganizationId),
                        CONSTRAINT FK_RecursiveGroupMembers_Groups_GroupId_OrganizationId
                            FOREIGN KEY (GroupId, OrganizationId) REFERENCES dbo.Groups(Id, OrganizationId),
                        CONSTRAINT FK_RecursiveGroupMembers_Groups_RootGroupId_RootOrganizationId
                            FOREIGN KEY (RootGroupId, RootOrganizationId) REFERENCES dbo.Groups(Id, OrganizationId),
                        CONSTRAINT FK_RecursiveGroupMembers_Organizations_OrganizationId
                            FOREIGN KEY (OrganizationId) REFERENCES dbo.Organizations(Id)
                    );

                    CREATE NONCLUSTERED INDEX IX_RGMP_GroupId_OrgId
                        ON dbo.RecursiveGroupMembers (GroupId, OrganizationId);
                    CREATE NONCLUSTERED INDEX IX_RecursiveGroupMembers_OrganizationId
                        ON dbo.RecursiveGroupMembers (OrganizationId);
                END");

            // Drop indexes that may already exist from the old idempotent zz_CompositeIndexes.sql
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_StackRoleAssign_Group_PrincipalFirst] ON [StackRoleAssignments]");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_StackRoleAssign_SP_StackFirst] ON [StackRoleAssignments]");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_StackRoleAssign_User_StackFirst] ON [StackRoleAssignments]");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_StackRoleAssign_UserSP_StackFirst] ON [StackRoleAssignments]");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_RunnerRoleAssign_Runner_Principal_Org_Role] ON [RunnerRoleAssignments]");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_OrgRoleAssign_Principal_Org_Role] ON [OrganizationRoleAssignments]");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_NsRoleAssign_Ns_Principal_Org_Role] ON [NamespaceRoleAssignments]");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_NsRoleAssign_Group_PrincipalFirst] ON [NamespaceRoleAssignments]");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_NsRoleAssign_SP_NsFirst] ON [NamespaceRoleAssignments]");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_NsRoleAssign_User_NsFirst] ON [NamespaceRoleAssignments]");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_NsRoleAssign_UserSP_NsFirst] ON [NamespaceRoleAssignments]");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_ModRoleAssign_Mod_Principal_Org_Role] ON [ModuleRoleAssignments]");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_IntegRoleAssign_Integ_Principal_Org_Role] ON [IntegrationRoleAssignments]");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_GroupMembers_Principal_Org_Disc] ON [GroupMembers]");
            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_AgentRoleAssign_Agent_Principal_Org_Role] ON [AgentRoleAssignments]");

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssign_UserSP_StackFirst",
                table: "StackRoleAssignments",
                columns: new[] { "StackId", "OrganizationId", "PrincipalId", "RoleName" },
                filter: "[PrincipalDiscriminator] IN ('User', 'ServicePrincipal')");

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssign_Group_PrincipalFirst",
                table: "StackRoleAssignments",
                columns: new[] { "PrincipalId", "StackId", "OrganizationId", "RoleName" },
                filter: "[PrincipalDiscriminator] = 'Group'");

            migrationBuilder.CreateIndex(
                name: "IX_NsRoleAssign_UserSP_NsFirst",
                table: "NamespaceRoleAssignments",
                columns: new[] { "NamespaceId", "OrganizationId", "PrincipalId", "RoleName" },
                filter: "[PrincipalDiscriminator] IN ('User', 'ServicePrincipal')");

            migrationBuilder.CreateIndex(
                name: "IX_NsRoleAssign_Group_PrincipalFirst",
                table: "NamespaceRoleAssignments",
                columns: new[] { "PrincipalId", "NamespaceId", "OrganizationId", "RoleName" },
                filter: "[PrincipalDiscriminator] = 'Group'");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssign_Runner_Principal_Org_Role",
                table: "RunnerRoleAssignments",
                columns: new[] { "RunnerId", "OrganizationId", "PrincipalId", "RoleName" });

            migrationBuilder.CreateIndex(
                name: "IX_OrgRoleAssign_Principal_Org_Role",
                table: "OrganizationRoleAssignments",
                columns: new[] { "PrincipalId", "OrganizationId", "RoleName" });

            migrationBuilder.CreateIndex(
                name: "IX_ModRoleAssign_Mod_Principal_Org_Role",
                table: "ModuleRoleAssignments",
                columns: new[] { "ModuleId", "OrganizationId", "PrincipalId", "RoleName" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegRoleAssign_Integ_Principal_Org_Role",
                table: "IntegrationRoleAssignments",
                columns: new[] { "IntegrationId", "OrganizationId", "PrincipalId", "RoleName" });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMembers_Principal_Org_Disc",
                table: "GroupMembers",
                columns: new[] { "PrincipalId", "OrganizationId", "GroupMemberDiscriminator" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssign_Agent_Principal_Org_Role",
                table: "AgentRoleAssignments",
                columns: new[] { "AgentId", "OrganizationId", "PrincipalId", "RoleName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecursiveGroupMembers");

            migrationBuilder.DropIndex(
                name: "IX_StackRoleAssign_UserSP_StackFirst",
                table: "StackRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_StackRoleAssign_Group_PrincipalFirst",
                table: "StackRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_NsRoleAssign_UserSP_NsFirst",
                table: "NamespaceRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_NsRoleAssign_Group_PrincipalFirst",
                table: "NamespaceRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_RunnerRoleAssign_Runner_Principal_Org_Role",
                table: "RunnerRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_OrgRoleAssign_Principal_Org_Role",
                table: "OrganizationRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ModRoleAssign_Mod_Principal_Org_Role",
                table: "ModuleRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_IntegRoleAssign_Integ_Principal_Org_Role",
                table: "IntegrationRoleAssignments");

            migrationBuilder.DropIndex(
                name: "IX_GroupMembers_Principal_Org_Disc",
                table: "GroupMembers");

            migrationBuilder.DropIndex(
                name: "IX_AgentRoleAssign_Agent_Principal_Org_Role",
                table: "AgentRoleAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_StackRoleAssignments_StackId_OrganizationId",
                table: "StackRoleAssignments",
                columns: new[] { "StackId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRoleAssignments_RunnerId_OrganizationId",
                table: "RunnerRoleAssignments",
                columns: new[] { "RunnerId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_NamespaceRoleAssignments_NamespaceId_OrganizationId",
                table: "NamespaceRoleAssignments",
                columns: new[] { "NamespaceId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleRoleAssignments_ModuleId_OrganizationId",
                table: "ModuleRoleAssignments",
                columns: new[] { "ModuleId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationRoleAssignments_IntegrationId_OrganizationId",
                table: "IntegrationRoleAssignments",
                columns: new[] { "IntegrationId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRoleAssignments_AgentId_OrganizationId",
                table: "AgentRoleAssignments",
                columns: new[] { "AgentId", "OrganizationId" });
        }
    }
}
