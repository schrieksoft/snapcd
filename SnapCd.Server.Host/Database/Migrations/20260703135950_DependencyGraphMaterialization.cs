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
    public partial class DependencyGraphMaterialization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. DependencyEdges — flattened direct edges from DependsOnModules + ModuleInputs
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DependencyEdges')
                CREATE TABLE DependencyEdges (
                    DefinedModuleId     UNIQUEIDENTIFIER NOT NULL,
                    ReferencedModuleId  UNIQUEIDENTIFIER NOT NULL,
                    OrganizationId      UNIQUEIDENTIFIER NOT NULL,
                    CONSTRAINT PK_DependencyEdges PRIMARY KEY CLUSTERED (DefinedModuleId, ReferencedModuleId)
                );

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DependencyEdges_ReferencedModuleId' AND object_id = OBJECT_ID('DependencyEdges'))
                CREATE NONCLUSTERED INDEX IX_DependencyEdges_ReferencedModuleId
                    ON DependencyEdges (ReferencedModuleId);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DependencyEdges_OrganizationId' AND object_id = OBJECT_ID('DependencyEdges'))
                CREATE NONCLUSTERED INDEX IX_DependencyEdges_OrganizationId
                    ON DependencyEdges (OrganizationId);
            ");

            // 2. RecursiveDependencyEdges — transitive closure of the dependency graph
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RecursiveDependencyEdges')
                CREATE TABLE RecursiveDependencyEdges (
                    RootModuleId           UNIQUEIDENTIFIER NOT NULL,
                    RootOrganizationId     UNIQUEIDENTIFIER NOT NULL,
                    RootModuleName         NVARCHAR(450) NOT NULL,
                    RootNamespaceId        UNIQUEIDENTIFIER NOT NULL,
                    RootNamespaceName      NVARCHAR(450) NOT NULL,
                    RootStackId            UNIQUEIDENTIFIER NOT NULL,
                    RootStackName          NVARCHAR(450) NOT NULL,
                    RootDisplayName        NVARCHAR(MAX) NOT NULL,
                    DefinedModuleId        UNIQUEIDENTIFIER NOT NULL,
                    DefinedOrganizationId  UNIQUEIDENTIFIER NOT NULL,
                    DefinedModuleName      NVARCHAR(450) NOT NULL,
                    DefinedNamespaceId     UNIQUEIDENTIFIER NOT NULL,
                    DefinedNamespaceName   NVARCHAR(450) NOT NULL,
                    DefinedStackId         UNIQUEIDENTIFIER NOT NULL,
                    DefinedStackName       NVARCHAR(450) NOT NULL,
                    DefinedDisplayName     NVARCHAR(MAX) NOT NULL,
                    ReferencedModuleId     UNIQUEIDENTIFIER NOT NULL,
                    ReferencedOrganizationId UNIQUEIDENTIFIER NOT NULL,
                    ReferencedModuleName   NVARCHAR(450) NOT NULL,
                    ReferencedNamespaceId  UNIQUEIDENTIFIER NOT NULL,
                    ReferencedNamespaceName NVARCHAR(450) NOT NULL,
                    ReferencedStackId      UNIQUEIDENTIFIER NOT NULL,
                    ReferencedStackName    NVARCHAR(450) NOT NULL,
                    ReferencedDisplayName  NVARCHAR(MAX) NOT NULL,
                    Depth                  INT NOT NULL,
                    OrganizationId         UNIQUEIDENTIFIER NOT NULL,
                    Direction              TINYINT NOT NULL
                );
            ");

            // Filtered indexes for RecursiveDependencyEdges
            migrationBuilder.Sql(@"
                SET QUOTED_IDENTIFIER ON;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecursiveDependencyEdges_Apply_RootModule' AND object_id = OBJECT_ID('RecursiveDependencyEdges'))
                CREATE NONCLUSTERED INDEX IX_RecursiveDependencyEdges_Apply_RootModule
                    ON RecursiveDependencyEdges (Direction, RootModuleId)
                    INCLUDE (DefinedModuleId, ReferencedModuleId, Depth, OrganizationId)
                    WHERE Direction = 1;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecursiveDependencyEdges_Destroy_RootModule' AND object_id = OBJECT_ID('RecursiveDependencyEdges'))
                CREATE NONCLUSTERED INDEX IX_RecursiveDependencyEdges_Destroy_RootModule
                    ON RecursiveDependencyEdges (Direction, RootModuleId)
                    INCLUDE (DefinedModuleId, ReferencedModuleId, Depth, OrganizationId)
                    WHERE Direction = 2;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecursiveDependencyEdges_Apply_DefinedNamespace' AND object_id = OBJECT_ID('RecursiveDependencyEdges'))
                CREATE NONCLUSTERED INDEX IX_RecursiveDependencyEdges_Apply_DefinedNamespace
                    ON RecursiveDependencyEdges (Direction, DefinedNamespaceId)
                    INCLUDE (RootModuleId, DefinedModuleId, ReferencedModuleId, Depth, OrganizationId)
                    WHERE Direction = 1;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecursiveDependencyEdges_Apply_ReferencedNamespace' AND object_id = OBJECT_ID('RecursiveDependencyEdges'))
                CREATE NONCLUSTERED INDEX IX_RecursiveDependencyEdges_Apply_ReferencedNamespace
                    ON RecursiveDependencyEdges (Direction, ReferencedNamespaceId)
                    INCLUDE (RootModuleId, DefinedModuleId, ReferencedModuleId, Depth, OrganizationId)
                    WHERE Direction = 1;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecursiveDependencyEdges_Destroy_DefinedNamespace' AND object_id = OBJECT_ID('RecursiveDependencyEdges'))
                CREATE NONCLUSTERED INDEX IX_RecursiveDependencyEdges_Destroy_DefinedNamespace
                    ON RecursiveDependencyEdges (Direction, DefinedNamespaceId)
                    INCLUDE (RootModuleId, DefinedModuleId, ReferencedModuleId, Depth, OrganizationId)
                    WHERE Direction = 2;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecursiveDependencyEdges_Destroy_ReferencedNamespace' AND object_id = OBJECT_ID('RecursiveDependencyEdges'))
                CREATE NONCLUSTERED INDEX IX_RecursiveDependencyEdges_Destroy_ReferencedNamespace
                    ON RecursiveDependencyEdges (Direction, ReferencedNamespaceId)
                    INCLUDE (RootModuleId, DefinedModuleId, ReferencedModuleId, Depth, OrganizationId)
                    WHERE Direction = 2;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecursiveDependencyEdges_Apply_DefinedStack' AND object_id = OBJECT_ID('RecursiveDependencyEdges'))
                CREATE NONCLUSTERED INDEX IX_RecursiveDependencyEdges_Apply_DefinedStack
                    ON RecursiveDependencyEdges (Direction, DefinedStackId)
                    INCLUDE (RootModuleId, DefinedModuleId, ReferencedModuleId, Depth, OrganizationId)
                    WHERE Direction = 1;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecursiveDependencyEdges_Apply_ReferencedStack' AND object_id = OBJECT_ID('RecursiveDependencyEdges'))
                CREATE NONCLUSTERED INDEX IX_RecursiveDependencyEdges_Apply_ReferencedStack
                    ON RecursiveDependencyEdges (Direction, ReferencedStackId)
                    INCLUDE (RootModuleId, DefinedModuleId, ReferencedModuleId, Depth, OrganizationId)
                    WHERE Direction = 1;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecursiveDependencyEdges_Destroy_DefinedStack' AND object_id = OBJECT_ID('RecursiveDependencyEdges'))
                CREATE NONCLUSTERED INDEX IX_RecursiveDependencyEdges_Destroy_DefinedStack
                    ON RecursiveDependencyEdges (Direction, DefinedStackId)
                    INCLUDE (RootModuleId, DefinedModuleId, ReferencedModuleId, Depth, OrganizationId)
                    WHERE Direction = 2;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RecursiveDependencyEdges_Destroy_ReferencedStack' AND object_id = OBJECT_ID('RecursiveDependencyEdges'))
                CREATE NONCLUSTERED INDEX IX_RecursiveDependencyEdges_Destroy_ReferencedStack
                    ON RecursiveDependencyEdges (Direction, ReferencedStackId)
                    INCLUDE (RootModuleId, DefinedModuleId, ReferencedModuleId, Depth, OrganizationId)
                    WHERE Direction = 2;
            ");

            // 3. ModuleState — pre-materialized per-module state from ModuleJobs + ModuleSagas
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ModuleState')
                CREATE TABLE ModuleState (
                    ModuleId                    UNIQUEIDENTIFIER NOT NULL PRIMARY KEY CLUSTERED,
                    OrganizationId              UNIQUEIDENTIFIER NOT NULL,
                    IsRunning                   BIT NOT NULL DEFAULT 0,
                    LatestActualStateHeadline   NVARCHAR(MAX) NULL,
                    DesiredStateHeadline        NVARCHAR(MAX) NULL,
                    QueuedDesiredStateHeadline  NVARCHAR(MAX) NULL
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_ModuleSagas_ModuleState");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_ModuleJobs_ModuleState");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_RecomputeModuleState");
            migrationBuilder.Sql("DROP TABLE IF EXISTS ModuleState");

            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_DependencyEdges_RecursiveClosure");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_RecomputeRecursiveDependencyEdges");
            migrationBuilder.Sql("DROP TABLE IF EXISTS RecursiveDependencyEdges");

            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_ModuleInputs_DependencyEdges");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_DependsOnModules_DependencyEdges");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_RecomputeDependencyEdges");
            migrationBuilder.Sql("DROP TABLE IF EXISTS DependencyEdges");
        }
    }
}
