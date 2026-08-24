-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- ==========================================================================
-- Dependency graph materialization infrastructure.
--
-- Stored procedures, triggers, and initial population for three
-- trigger-maintained tables (created by migration):
--   1. DependencyEdges            - flattened direct edges
--   2. RecursiveDependencyEdges   - transitive closure
--   3. ModuleState                - pre-materialized per-module state
--
-- Must run BEFORE the view scripts (02-05).
-- ==========================================================================

-- ============================================================================
-- 1. Table type for passing module IDs to stored procedures
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.types WHERE name = 'GuidList' AND is_table_type = 1)
BEGIN
    CREATE TYPE dbo.GuidList AS TABLE (Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY);
END;
GO

-- Edges are identified by their key pair so incremental updates lock only the rows that changed.
IF NOT EXISTS (SELECT 1 FROM sys.types WHERE name = 'ModuleEdgeList' AND is_table_type = 1)
BEGIN
    CREATE TYPE dbo.ModuleEdgeList AS TABLE (
        DefinedModuleId UNIQUEIDENTIFIER NOT NULL,
        ReferencedModuleId UNIQUEIDENTIFIER NOT NULL,
        PRIMARY KEY (DefinedModuleId, ReferencedModuleId));
END;
GO

-- ============================================================================
-- 2. Stored procedure to recompute DependencyEdges (full rebuild)
-- ============================================================================

CREATE OR ALTER PROCEDURE sp_RecomputeDependencyEdges
AS
BEGIN
    SET NOCOUNT ON;

    MERGE DependencyEdges AS target
    USING (
        SELECT ModuleId AS DefinedModuleId, DependsOnModuleId AS ReferencedModuleId, OrganizationId
        FROM DependsOnModules
        UNION
        SELECT ModuleId AS DefinedModuleId, OutputModuleId AS ReferencedModuleId, OrganizationId
        FROM ModuleInputs
        WHERE Discriminator IN ('ModuleEnvVarFromOutput', 'ModuleParamFromOutput', 'ModuleParamFromOutputSet')
    ) AS source
    ON target.DefinedModuleId = source.DefinedModuleId AND target.ReferencedModuleId = source.ReferencedModuleId
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (DefinedModuleId, ReferencedModuleId, OrganizationId)
        VALUES (source.DefinedModuleId, source.ReferencedModuleId, source.OrganizationId)
    WHEN NOT MATCHED BY SOURCE THEN
        DELETE;
END;
GO

-- ============================================================================
-- 3. Stored procedure to incrementally update DependencyEdges for specific modules
-- ============================================================================

CREATE OR ALTER PROCEDURE sp_UpdateDependencyEdgesForModules
    @AffectedEdges dbo.ModuleEdgeList READONLY
AS
BEGIN
    SET NOCOUNT ON;

    -- The desired edges among the pairs being reconciled.
    DECLARE @desired TABLE (
        DefinedModuleId UNIQUEIDENTIFIER NOT NULL,
        ReferencedModuleId UNIQUEIDENTIFIER NOT NULL,
        OrganizationId UNIQUEIDENTIFIER NOT NULL,
        PRIMARY KEY (DefinedModuleId, ReferencedModuleId));

    INSERT INTO @desired (DefinedModuleId, ReferencedModuleId, OrganizationId)
    SELECT s.DefinedModuleId, s.ReferencedModuleId, MIN(s.OrganizationId)
    FROM (
        SELECT ModuleId AS DefinedModuleId, DependsOnModuleId AS ReferencedModuleId, OrganizationId
        FROM DependsOnModules
        UNION
        SELECT ModuleId AS DefinedModuleId, OutputModuleId AS ReferencedModuleId, OrganizationId
        FROM ModuleInputs
        WHERE Discriminator IN ('ModuleEnvVarFromOutput', 'ModuleParamFromOutput', 'ModuleParamFromOutputSet')
    ) s
    JOIN @AffectedEdges e
      ON e.DefinedModuleId = s.DefinedModuleId AND e.ReferencedModuleId = s.ReferencedModuleId
    GROUP BY s.DefinedModuleId, s.ReferencedModuleId;

    -- Delete, update, then insert, each seeking to single keys.
    DELETE t
    FROM DependencyEdges t
    JOIN @AffectedEdges e
      ON e.DefinedModuleId = t.DefinedModuleId AND e.ReferencedModuleId = t.ReferencedModuleId
    WHERE NOT EXISTS (SELECT 1 FROM @desired d
                      WHERE d.DefinedModuleId = t.DefinedModuleId
                        AND d.ReferencedModuleId = t.ReferencedModuleId);

    UPDATE t
    SET OrganizationId = d.OrganizationId
    FROM DependencyEdges t
    JOIN @desired d
      ON d.DefinedModuleId = t.DefinedModuleId AND d.ReferencedModuleId = t.ReferencedModuleId
    WHERE t.OrganizationId <> d.OrganizationId;

    INSERT INTO DependencyEdges (DefinedModuleId, ReferencedModuleId, OrganizationId)
    SELECT d.DefinedModuleId, d.ReferencedModuleId, d.OrganizationId
    FROM @desired d
    WHERE NOT EXISTS (SELECT 1 FROM DependencyEdges t WITH (UPDLOCK, HOLDLOCK)
                      WHERE t.DefinedModuleId = d.DefinedModuleId
                        AND t.ReferencedModuleId = d.ReferencedModuleId)
    ORDER BY d.DefinedModuleId, d.ReferencedModuleId;
END;
GO

-- ============================================================================
-- 4. Triggers on DependsOnModules and ModuleInputs to maintain DependencyEdges
-- ============================================================================

CREATE OR ALTER TRIGGER trg_DependsOnModules_DependencyEdges
ON DependsOnModules
AFTER INSERT, DELETE, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @affected dbo.ModuleEdgeList;
    INSERT INTO @affected (DefinedModuleId, ReferencedModuleId)
    SELECT ModuleId, DependsOnModuleId FROM inserted WHERE DependsOnModuleId IS NOT NULL
    UNION
    SELECT ModuleId, DependsOnModuleId FROM deleted WHERE DependsOnModuleId IS NOT NULL;

    EXEC sp_UpdateDependencyEdgesForModules @affected;
END;
GO

CREATE OR ALTER TRIGGER trg_ModuleInputs_DependencyEdges
ON ModuleInputs
AFTER INSERT, DELETE, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 FROM inserted
        WHERE Discriminator IN ('ModuleEnvVarFromOutput', 'ModuleParamFromOutput', 'ModuleParamFromOutputSet')
    ) AND NOT EXISTS (
        SELECT 1 FROM deleted
        WHERE Discriminator IN ('ModuleEnvVarFromOutput', 'ModuleParamFromOutput', 'ModuleParamFromOutputSet')
    )
        RETURN;

    DECLARE @affected dbo.ModuleEdgeList;
    INSERT INTO @affected (DefinedModuleId, ReferencedModuleId)
    SELECT ModuleId, OutputModuleId FROM inserted
    WHERE Discriminator IN ('ModuleEnvVarFromOutput', 'ModuleParamFromOutput', 'ModuleParamFromOutputSet')
      AND OutputModuleId IS NOT NULL
    UNION
    SELECT ModuleId, OutputModuleId FROM deleted
    WHERE Discriminator IN ('ModuleEnvVarFromOutput', 'ModuleParamFromOutput', 'ModuleParamFromOutputSet')
      AND OutputModuleId IS NOT NULL;

    EXEC sp_UpdateDependencyEdgesForModules @affected;
END;
GO

-- ============================================================================
-- 5. Stored procedure to recompute the transitive closure
--    @StackId NULL = full rebuild, non-NULL = stack-scoped rebuild
-- ============================================================================

CREATE OR ALTER PROCEDURE sp_RecomputeRecursiveDependencyEdges
    @StackId UNIQUEIDENTIFIER = NULL,
    @AffectedRoots dbo.GuidList READONLY
AS
BEGIN
    SET NOCOUNT ON;

    -- An empty @AffectedRoots means "every root in scope"; callers that know which roots a change
    -- can reach pass them so the rebuild touches those instead of the whole stack.
    DECLARE @scoped BIT = CASE WHEN EXISTS (SELECT 1 FROM @AffectedRoots) THEN 1 ELSE 0 END;

    -- A rebuild rewrites the closure for the whole stack, so two of them for one stack deadlock
    -- against each other and against the edge writes that triggered them. Serialize per stack.
    IF @StackId IS NOT NULL AND @@TRANCOUNT > 0
    BEGIN
        DECLARE @lockRes NVARCHAR(255) = 'RecursiveClosure:' + CAST(@StackId AS CHAR(36));
        DECLARE @rc INT;
        EXEC @rc = sp_getapplock @Resource = @lockRes, @LockMode = 'Exclusive',
                                 @LockOwner = 'Transaction', @LockTimeout = 30000;
        IF @rc < 0 THROW 51000, 'Timed out acquiring dependency closure lock.', 1;
    END

    CREATE TABLE #ModuleMap (ModuleId UNIQUEIDENTIFIER PRIMARY KEY, Seq INT NOT NULL);
    INSERT INTO #ModuleMap (ModuleId, Seq)
    SELECT m.Id, ROW_NUMBER() OVER (ORDER BY m.Id)
    FROM Modules m
    INNER JOIN Namespaces ns ON ns.Id = m.NamespaceId
    WHERE @StackId IS NULL OR ns.StackId = @StackId;

    -- Roots whose closure rows are rebuilt below. Traversal still needs every module in the
    -- stack in #ModuleMap, because a path from an affected root may pass through any of them.
    CREATE TABLE #Roots (ModuleId UNIQUEIDENTIFIER PRIMARY KEY);
    IF @scoped = 1
        INSERT INTO #Roots (ModuleId)
        SELECT r.Id FROM @AffectedRoots r WHERE EXISTS (SELECT 1 FROM #ModuleMap mm WHERE mm.ModuleId = r.Id);
    ELSE
        INSERT INTO #Roots (ModuleId) SELECT ModuleId FROM #ModuleMap;

    IF @StackId IS NULL AND @scoped = 0
        TRUNCATE TABLE RecursiveDependencyEdges;
    ELSE
        DELETE rde
        FROM RecursiveDependencyEdges rde
        WHERE rde.RootModuleId IN (SELECT ModuleId FROM #Roots);

    -- Apply direction: root = DefinedModuleId, walk Defined->Referenced
    WITH ApplyClosure AS (
        SELECT
            de.DefinedModuleId AS RootModuleId,
            de.DefinedModuleId,
            de.ReferencedModuleId,
            de.OrganizationId,
            1 AS Depth,
            CAST('|' + CAST(dm.Seq AS VARCHAR(10)) + '|' + CAST(rm.Seq AS VARCHAR(10)) + '|' AS VARCHAR(MAX)) AS VisitedPath
        FROM DependencyEdges de
        INNER JOIN #ModuleMap dm ON dm.ModuleId = de.DefinedModuleId
        INNER JOIN #ModuleMap rm ON rm.ModuleId = de.ReferencedModuleId
        WHERE EXISTS (SELECT 1 FROM #Roots rt WHERE rt.ModuleId = de.DefinedModuleId)

        UNION ALL

        SELECT
            ac.RootModuleId,
            de.DefinedModuleId,
            de.ReferencedModuleId,
            de.OrganizationId,
            ac.Depth + 1,
            CAST(ac.VisitedPath + CAST(rm.Seq AS VARCHAR(10)) + '|' AS VARCHAR(MAX))
        FROM ApplyClosure ac
        INNER JOIN DependencyEdges de ON ac.ReferencedModuleId = de.DefinedModuleId
        INNER JOIN #ModuleMap rm ON rm.ModuleId = de.ReferencedModuleId
        WHERE CHARINDEX('|' + CAST(rm.Seq AS VARCHAR(10)) + '|', ac.VisitedPath) = 0
    )
    INSERT INTO RecursiveDependencyEdges (
        RootModuleId, RootOrganizationId, RootModuleName, RootNamespaceId, RootNamespaceName, RootStackId, RootStackName, RootDisplayName,
        DefinedModuleId, DefinedOrganizationId, DefinedModuleName, DefinedNamespaceId, DefinedNamespaceName, DefinedStackId, DefinedStackName, DefinedDisplayName,
        ReferencedModuleId, ReferencedOrganizationId, ReferencedModuleName, ReferencedNamespaceId, ReferencedNamespaceName, ReferencedStackId, ReferencedStackName, ReferencedDisplayName,
        Depth, OrganizationId, Direction)
    SELECT
        ac.RootModuleId, m_root.OrganizationId, m_root.Name, m_root.NamespaceId, ns_root.Name, ns_root.StackId, st_root.Name,
        CONCAT(st_root.Name, '/', ns_root.Name, '/', m_root.Name),
        ac.DefinedModuleId, m_def.OrganizationId, m_def.Name, m_def.NamespaceId, ns_def.Name, ns_def.StackId, st_def.Name,
        CONCAT(st_def.Name, '/', ns_def.Name, '/', m_def.Name),
        ac.ReferencedModuleId, m_ref.OrganizationId, m_ref.Name, m_ref.NamespaceId, ns_ref.Name, ns_ref.StackId, st_ref.Name,
        CONCAT(st_ref.Name, '/', ns_ref.Name, '/', m_ref.Name),
        ac.Depth, ac.OrganizationId, 1
    FROM ApplyClosure ac
    INNER JOIN Modules m_root ON m_root.Id = ac.RootModuleId
    INNER JOIN Namespaces ns_root ON ns_root.Id = m_root.NamespaceId
    INNER JOIN Stacks st_root ON st_root.Id = ns_root.StackId
    INNER JOIN Modules m_def ON m_def.Id = ac.DefinedModuleId
    INNER JOIN Namespaces ns_def ON ns_def.Id = m_def.NamespaceId
    INNER JOIN Stacks st_def ON st_def.Id = ns_def.StackId
    INNER JOIN Modules m_ref ON m_ref.Id = ac.ReferencedModuleId
    INNER JOIN Namespaces ns_ref ON ns_ref.Id = m_ref.NamespaceId
    INNER JOIN Stacks st_ref ON st_ref.Id = ns_ref.StackId
    OPTION (MAXRECURSION 0);

    -- Destroy direction: root = ReferencedModuleId, walk Referenced->Defined
    WITH DestroyClosure AS (
        SELECT
            de.ReferencedModuleId AS RootModuleId,
            de.DefinedModuleId,
            de.ReferencedModuleId,
            de.OrganizationId,
            1 AS Depth,
            CAST('|' + CAST(rm.Seq AS VARCHAR(10)) + '|' + CAST(dm.Seq AS VARCHAR(10)) + '|' AS VARCHAR(MAX)) AS VisitedPath
        FROM DependencyEdges de
        INNER JOIN #ModuleMap dm ON dm.ModuleId = de.DefinedModuleId
        INNER JOIN #ModuleMap rm ON rm.ModuleId = de.ReferencedModuleId
        WHERE EXISTS (SELECT 1 FROM #Roots rt WHERE rt.ModuleId = de.ReferencedModuleId)

        UNION ALL

        SELECT
            dc.RootModuleId,
            de.DefinedModuleId,
            de.ReferencedModuleId,
            de.OrganizationId,
            dc.Depth + 1,
            CAST(dc.VisitedPath + CAST(dm.Seq AS VARCHAR(10)) + '|' AS VARCHAR(MAX))
        FROM DestroyClosure dc
        INNER JOIN DependencyEdges de ON dc.DefinedModuleId = de.ReferencedModuleId
        INNER JOIN #ModuleMap dm ON dm.ModuleId = de.DefinedModuleId
        WHERE CHARINDEX('|' + CAST(dm.Seq AS VARCHAR(10)) + '|', dc.VisitedPath) = 0
    )
    INSERT INTO RecursiveDependencyEdges (
        RootModuleId, RootOrganizationId, RootModuleName, RootNamespaceId, RootNamespaceName, RootStackId, RootStackName, RootDisplayName,
        DefinedModuleId, DefinedOrganizationId, DefinedModuleName, DefinedNamespaceId, DefinedNamespaceName, DefinedStackId, DefinedStackName, DefinedDisplayName,
        ReferencedModuleId, ReferencedOrganizationId, ReferencedModuleName, ReferencedNamespaceId, ReferencedNamespaceName, ReferencedStackId, ReferencedStackName, ReferencedDisplayName,
        Depth, OrganizationId, Direction)
    SELECT
        dc.RootModuleId, m_root.OrganizationId, m_root.Name, m_root.NamespaceId, ns_root.Name, ns_root.StackId, st_root.Name,
        CONCAT(st_root.Name, '/', ns_root.Name, '/', m_root.Name),
        dc.DefinedModuleId, m_def.OrganizationId, m_def.Name, m_def.NamespaceId, ns_def.Name, ns_def.StackId, st_def.Name,
        CONCAT(st_def.Name, '/', ns_def.Name, '/', m_def.Name),
        dc.ReferencedModuleId, m_ref.OrganizationId, m_ref.Name, m_ref.NamespaceId, ns_ref.Name, ns_ref.StackId, st_ref.Name,
        CONCAT(st_ref.Name, '/', ns_ref.Name, '/', m_ref.Name),
        dc.Depth, dc.OrganizationId, 2
    FROM DestroyClosure dc
    INNER JOIN Modules m_root ON m_root.Id = dc.RootModuleId
    INNER JOIN Namespaces ns_root ON ns_root.Id = m_root.NamespaceId
    INNER JOIN Stacks st_root ON st_root.Id = ns_root.StackId
    INNER JOIN Modules m_def ON m_def.Id = dc.DefinedModuleId
    INNER JOIN Namespaces ns_def ON ns_def.Id = m_def.NamespaceId
    INNER JOIN Stacks st_def ON st_def.Id = ns_def.StackId
    INNER JOIN Modules m_ref ON m_ref.Id = dc.ReferencedModuleId
    INNER JOIN Namespaces ns_ref ON ns_ref.Id = m_ref.NamespaceId
    INNER JOIN Stacks st_ref ON st_ref.Id = ns_ref.StackId
    OPTION (MAXRECURSION 0);

    DROP TABLE #ModuleMap;
    DROP TABLE #Roots;
END;
GO

-- ============================================================================
-- 6. Trigger on DependencyEdges to recompute transitive closure (stack-scoped)
-- ============================================================================

CREATE OR ALTER TRIGGER trg_DependencyEdges_RecursiveClosure
ON DependencyEdges
AFTER INSERT, DELETE, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @stackIds TABLE (StackId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY);
    INSERT INTO @stackIds (StackId)
    SELECT DISTINCT ns.StackId
    FROM (
        SELECT DefinedModuleId FROM inserted
        UNION
        SELECT DefinedModuleId FROM deleted
    ) affected
    INNER JOIN Modules m ON m.Id = affected.DefinedModuleId
    INNER JOIN Namespaces ns ON ns.Id = m.NamespaceId;

    -- Roots whose closure a changed edge can invalidate: apply-direction roots reach the defined
    -- module, destroy-direction roots are reachable from the referenced one, and both endpoints are
    -- roots of their own. Read before the rebuild deletes anything, since it uses the old closure.
    DECLARE @affectedRoots dbo.GuidList;
    INSERT INTO @affectedRoots (Id)
    SELECT DISTINCT Id FROM (
        SELECT DefinedModuleId AS Id FROM inserted
        UNION SELECT DefinedModuleId FROM deleted
        UNION SELECT ReferencedModuleId FROM inserted
        UNION SELECT ReferencedModuleId FROM deleted
        UNION
        SELECT r.RootModuleId FROM RecursiveDependencyEdges r
        WHERE r.Direction = 1
          AND r.ReferencedModuleId IN (SELECT DefinedModuleId FROM inserted
                                       UNION SELECT DefinedModuleId FROM deleted)
        UNION
        SELECT r.RootModuleId FROM RecursiveDependencyEdges r
        WHERE r.Direction = 2
          AND r.DefinedModuleId IN (SELECT ReferencedModuleId FROM inserted
                                    UNION SELECT ReferencedModuleId FROM deleted)
    ) x
    WHERE Id IS NOT NULL;

    DECLARE @stackId UNIQUEIDENTIFIER;
    DECLARE stack_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT StackId FROM @stackIds;

    OPEN stack_cursor;
    FETCH NEXT FROM stack_cursor INTO @stackId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC sp_RecomputeRecursiveDependencyEdges @StackId = @stackId, @AffectedRoots = @affectedRoots;
        FETCH NEXT FROM stack_cursor INTO @stackId;
    END;
    CLOSE stack_cursor;
    DEALLOCATE stack_cursor;
END;
GO

-- ============================================================================
-- 7. Stored procedure to fully recompute ModuleState
-- ============================================================================

CREATE OR ALTER PROCEDURE sp_RecomputeModuleState
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE ModuleState;

    WITH CurrentJobs AS (
        SELECT mj.ModuleId,
            ROW_NUMBER() OVER (PARTITION BY mj.ModuleId ORDER BY mj.TimestampStart DESC) AS rn
        FROM ModuleJobs mj WHERE mj.IsCurrent = 1
    ),
    LatestModuleJobs AS (
        SELECT mj.ModuleId,
            COALESCE(mj.ActualStateHeadline, REPLACE(mj.JobType, 'JobSaga', '') + mj.Status) AS ActualStateHeadline,
            ROW_NUMBER() OVER (PARTITION BY mj.ModuleId ORDER BY mj.TimestampEnd DESC) AS rn
        FROM ModuleJobs mj WHERE mj.TimestampEnd IS NOT NULL
    )
    INSERT INTO ModuleState (ModuleId, OrganizationId, IsRunning, LatestActualStateHeadline, DesiredStateHeadline, QueuedDesiredStateHeadline)
    SELECT
        m.Id,
        m.OrganizationId,
        CAST(CASE WHEN cj.ModuleId IS NOT NULL THEN 1 ELSE 0 END AS BIT),
        lj.ActualStateHeadline,
        ms.DesiredStateHeadline,
        ms.QueuedDesiredStateHeadline
    FROM Modules m
    LEFT JOIN CurrentJobs cj ON cj.ModuleId = m.Id AND cj.rn = 1
    LEFT JOIN LatestModuleJobs lj ON lj.ModuleId = m.Id AND lj.rn = 1
    LEFT JOIN ModuleSagas ms ON ms.CorrelationId = m.Id;
END;
GO

-- ============================================================================
-- 8. Trigger on ModuleJobs to maintain ModuleState
-- ============================================================================

CREATE OR ALTER TRIGGER trg_ModuleJobs_ModuleState
ON ModuleJobs
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Early exit for UPDATEs that touch none of the columns ModuleState is derived from.
    -- This matters for the log-append hot path (UPDATE ... SET Logs = ...): without it, every
    -- log write re-reads ModuleJobs inside the trigger, whose table scans take shared locks on
    -- rows other concurrent log writers hold exclusively - deadlocking writers of DIFFERENT jobs.
    -- EF Core only includes modified columns in the SET list, so UPDATE(col) is accurate here.
    IF EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted)
       AND NOT (UPDATE(ModuleId) OR UPDATE(IsCurrent) OR UPDATE(Status)
                OR UPDATE(ActualStateHeadline) OR UPDATE(JobType)
                OR UPDATE(TimestampStart) OR UPDATE(TimestampEnd))
        RETURN;

    SELECT DISTINCT ModuleId INTO #AffectedModules
    FROM (
        SELECT ModuleId FROM inserted
        UNION
        SELECT ModuleId FROM deleted
    ) x;

    INSERT INTO ModuleState (ModuleId, OrganizationId, IsRunning, LatestActualStateHeadline, DesiredStateHeadline, QueuedDesiredStateHeadline)
    SELECT am.ModuleId, m.OrganizationId, 0, NULL, NULL, NULL
    FROM #AffectedModules am
    INNER JOIN Modules m ON m.Id = am.ModuleId
    WHERE NOT EXISTS (SELECT 1 FROM ModuleState ms WHERE ms.ModuleId = am.ModuleId);

    UPDATE ms SET
        ms.IsRunning = CASE WHEN cj.ModuleId IS NOT NULL THEN 1 ELSE 0 END
    FROM ModuleState ms
    INNER JOIN #AffectedModules am ON am.ModuleId = ms.ModuleId
    LEFT JOIN (
        SELECT mj.ModuleId,
            ROW_NUMBER() OVER (PARTITION BY mj.ModuleId ORDER BY mj.TimestampStart DESC) AS rn
        FROM ModuleJobs mj
        WHERE mj.IsCurrent = 1
          AND mj.ModuleId IN (SELECT ModuleId FROM #AffectedModules)
    ) cj ON cj.ModuleId = ms.ModuleId AND cj.rn = 1;

    UPDATE ms SET
        ms.LatestActualStateHeadline = lj.ActualStateHeadline
    FROM ModuleState ms
    INNER JOIN #AffectedModules am ON am.ModuleId = ms.ModuleId
    LEFT JOIN (
        SELECT mj.ModuleId,
            COALESCE(mj.ActualStateHeadline, REPLACE(mj.JobType, 'JobSaga', '') + mj.Status) AS ActualStateHeadline,
            ROW_NUMBER() OVER (PARTITION BY mj.ModuleId ORDER BY mj.TimestampEnd DESC) AS rn
        FROM ModuleJobs mj
        WHERE mj.TimestampEnd IS NOT NULL
          AND mj.ModuleId IN (SELECT ModuleId FROM #AffectedModules)
    ) lj ON lj.ModuleId = ms.ModuleId AND lj.rn = 1;

    DROP TABLE #AffectedModules;
END;
GO

-- ============================================================================
-- 9. Trigger on ModuleSagas to maintain ModuleState
-- ============================================================================

CREATE OR ALTER TRIGGER trg_ModuleSagas_ModuleState
ON ModuleSagas
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ModuleState (ModuleId, OrganizationId, IsRunning, LatestActualStateHeadline, DesiredStateHeadline, QueuedDesiredStateHeadline)
    SELECT i.CorrelationId, i.OrganizationId, 0, NULL, i.DesiredStateHeadline, i.QueuedDesiredStateHeadline
    FROM inserted i
    WHERE NOT EXISTS (SELECT 1 FROM ModuleState ms WHERE ms.ModuleId = i.CorrelationId);

    UPDATE ms SET
        ms.DesiredStateHeadline = i.DesiredStateHeadline,
        ms.QueuedDesiredStateHeadline = i.QueuedDesiredStateHeadline
    FROM ModuleState ms
    INNER JOIN inserted i ON i.CorrelationId = ms.ModuleId;

    UPDATE ms SET
        ms.DesiredStateHeadline = NULL,
        ms.QueuedDesiredStateHeadline = NULL
    FROM ModuleState ms
    INNER JOIN deleted d ON d.CorrelationId = ms.ModuleId
    WHERE NOT EXISTS (SELECT 1 FROM inserted i WHERE i.CorrelationId = d.CorrelationId);
END;
GO

-- ============================================================================
-- 9b. Trigger on Modules to clean up ModuleState on module deletion
--
-- ModuleState has no FK to Modules (it is maintained by triggers), so deleting a
-- module used to leave an orphaned row behind: the cascade-delete of its ModuleJobs
-- fires trg_ModuleJobs_ModuleState, which nulls the state but keeps the row.
-- ============================================================================

CREATE OR ALTER TRIGGER trg_Modules_ModuleState
ON Modules
AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DELETE ms
    FROM ModuleState ms
    INNER JOIN deleted d ON d.Id = ms.ModuleId;

    -- A module carries a row from the moment it exists, with NULL state until it has run
    -- something. The job and saga triggers only insert on first activity, so without this
    -- the table is short of every never-run module and any INNER JOIN to it drops them.
    INSERT INTO ModuleState (ModuleId, OrganizationId, IsRunning, LatestActualStateHeadline, DesiredStateHeadline, QueuedDesiredStateHeadline)
    SELECT i.Id, i.OrganizationId, CAST(0 AS BIT), NULL, NULL, NULL
    FROM inserted i
    WHERE NOT EXISTS (SELECT 1 FROM ModuleState ms WHERE ms.ModuleId = i.Id);
END;
GO

-- One-time cleanup of orphaned rows accumulated before trg_Modules_ModuleState existed
-- (idempotent — a no-op once clean)
DELETE ms
FROM ModuleState ms
WHERE NOT EXISTS (SELECT 1 FROM Modules m WHERE m.Id = ms.ModuleId);
GO


-- ============================================================================
-- 10. Initial population (only on first deploy when tables are empty)
-- ============================================================================

IF NOT EXISTS (SELECT TOP 1 1 FROM DependencyEdges)
BEGIN
    EXEC sp_RecomputeDependencyEdges;
END;
GO

IF NOT EXISTS (SELECT TOP 1 1 FROM RecursiveDependencyEdges)
BEGIN
    EXEC sp_RecomputeRecursiveDependencyEdges;
END;
GO

IF NOT EXISTS (SELECT TOP 1 1 FROM ModuleState)
BEGIN
    EXEC sp_RecomputeModuleState;
END;
GO

-- Backfill modules created before the trigger inserted on creation. Runs after the initial
-- population above: seeding NULL-state rows first would satisfy that guard and suppress the
-- recompute, leaving every module without the state its jobs already establish.
INSERT INTO ModuleState (ModuleId, OrganizationId, IsRunning, LatestActualStateHeadline, DesiredStateHeadline, QueuedDesiredStateHeadline)
SELECT m.Id, m.OrganizationId, CAST(0 AS BIT), NULL, NULL, NULL
FROM Modules m
WHERE NOT EXISTS (SELECT 1 FROM ModuleState ms WHERE ms.ModuleId = m.Id);
GO
