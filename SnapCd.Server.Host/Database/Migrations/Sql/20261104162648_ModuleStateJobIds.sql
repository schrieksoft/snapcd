-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- ModuleState gains the identity of the two jobs its other columns are derived from, so a reader
-- can join ModuleJobs for any further job detail instead of this table growing a column per
-- attribute. CurrentModuleJobId is the in-flight job behind IsRunning; LatestModuleJobId is the
-- most recent finished job behind LatestActualStateHeadline. A Module mid-apply has both, and
-- they are different jobs.
--
-- No foreign key, matching DependencyEdges, RecursiveDependencyEdges and ModuleState's own
-- ModuleId: this cache is maintained by triggers and carries no referential constraints. The
-- ModuleJobs trigger below fires on DELETE as well, so a removed job leaves no dangling id.

SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ModuleState') AND name = 'CurrentModuleJobId')
    ALTER TABLE ModuleState ADD CurrentModuleJobId UNIQUEIDENTIFIER NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ModuleState') AND name = 'LatestModuleJobId')
    ALTER TABLE ModuleState ADD LatestModuleJobId UNIQUEIDENTIFIER NULL;
GO

-- ============================================================================
-- The headline a finished job reports when it recorded none of its own.
--
-- Mirrors CompleteModuleJobActivity: an Apply that completed is Applied, a Destroy is Destroyed,
-- and the failure statuses already match the enum's names. Concatenating the type and status
-- instead produced ApplyCompleted, which the ActualStateHeadline enum has no member for, and
-- reading that through EF throws.
--
-- A status with no headline of its own returns NULL, which the column and every reader allow.
-- ============================================================================

CREATE OR ALTER FUNCTION dbo.fn_ActualStateHeadline(@JobType NVARCHAR(200), @Status NVARCHAR(50))
RETURNS NVARCHAR(50)
AS
BEGIN
    DECLARE @Kind NVARCHAR(50) = REPLACE(@JobType, 'JobSaga', '');

    IF @Kind NOT IN ('Apply', 'Destroy') RETURN NULL;

    RETURN CASE
        WHEN @Status = 'Completed'
            THEN CASE WHEN @Kind = 'Apply' THEN 'Applied' ELSE 'Destroyed' END
        WHEN @Status IN ('Cancelled', 'NotApproved', 'Failed', 'Orphaned', 'Unknown', 'PolicyDenied')
            THEN @Kind + @Status
        ELSE NULL
    END;
END;
GO

-- ============================================================================
-- sp_RecomputeModuleState and the three ModuleState triggers, redefined to carry the two ids.
-- ============================================================================

CREATE OR ALTER PROCEDURE sp_RecomputeModuleState
AS
BEGIN
    SET NOCOUNT ON;

    TRUNCATE TABLE ModuleState;

    WITH CurrentJobs AS (
        SELECT mj.ModuleId, mj.Id,
            ROW_NUMBER() OVER (PARTITION BY mj.ModuleId ORDER BY mj.TimestampStart DESC) AS rn
        FROM ModuleJobs mj WHERE mj.IsCurrent = 1
    ),
    LatestModuleJobs AS (
        SELECT mj.ModuleId, mj.Id,
            COALESCE(mj.ActualStateHeadline, dbo.fn_ActualStateHeadline(mj.JobType, mj.Status)) AS ActualStateHeadline,
            ROW_NUMBER() OVER (PARTITION BY mj.ModuleId ORDER BY mj.JobNumber DESC) AS rn
        FROM ModuleJobs mj WHERE mj.TimestampEnd IS NOT NULL
    )
    INSERT INTO ModuleState (ModuleId, OrganizationId, IsRunning, CurrentModuleJobId, LatestModuleJobId, LatestActualStateHeadline, DesiredStateHeadline, QueuedDesiredStateHeadline)
    SELECT
        m.Id,
        m.OrganizationId,
        CAST(CASE WHEN cj.ModuleId IS NOT NULL THEN 1 ELSE 0 END AS BIT),
        cj.Id,
        lj.Id,
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
-- 10. Trigger on ModuleJobs to maintain ModuleState
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

    INSERT INTO ModuleState (ModuleId, OrganizationId, IsRunning, CurrentModuleJobId, LatestModuleJobId, LatestActualStateHeadline, DesiredStateHeadline, QueuedDesiredStateHeadline)
    SELECT am.ModuleId, m.OrganizationId, 0, NULL, NULL, NULL, NULL, NULL
    FROM #AffectedModules am
    INNER JOIN Modules m ON m.Id = am.ModuleId
    WHERE NOT EXISTS (SELECT 1 FROM ModuleState ms WHERE ms.ModuleId = am.ModuleId);

    UPDATE ms SET
        ms.IsRunning = CASE WHEN cj.ModuleId IS NOT NULL THEN 1 ELSE 0 END,
        ms.CurrentModuleJobId = cj.Id
    FROM ModuleState ms
    INNER JOIN #AffectedModules am ON am.ModuleId = ms.ModuleId
    LEFT JOIN (
        SELECT mj.ModuleId, mj.Id,
            ROW_NUMBER() OVER (PARTITION BY mj.ModuleId ORDER BY mj.TimestampStart DESC) AS rn
        FROM ModuleJobs mj
        WHERE mj.IsCurrent = 1
          AND mj.ModuleId IN (SELECT ModuleId FROM #AffectedModules)
    ) cj ON cj.ModuleId = ms.ModuleId AND cj.rn = 1;

    UPDATE ms SET
        ms.LatestActualStateHeadline = lj.ActualStateHeadline,
        ms.LatestModuleJobId = lj.Id
    FROM ModuleState ms
    INNER JOIN #AffectedModules am ON am.ModuleId = ms.ModuleId
    LEFT JOIN (
        SELECT mj.ModuleId, mj.Id,
            COALESCE(mj.ActualStateHeadline, dbo.fn_ActualStateHeadline(mj.JobType, mj.Status)) AS ActualStateHeadline,
            ROW_NUMBER() OVER (PARTITION BY mj.ModuleId ORDER BY mj.JobNumber DESC) AS rn
        FROM ModuleJobs mj
        WHERE mj.TimestampEnd IS NOT NULL
          AND mj.ModuleId IN (SELECT ModuleId FROM #AffectedModules)
    ) lj ON lj.ModuleId = ms.ModuleId AND lj.rn = 1;

    DROP TABLE #AffectedModules;
END;
GO

-- ============================================================================
-- 11. Trigger on ModuleSagas to maintain ModuleState
-- ============================================================================

CREATE OR ALTER TRIGGER trg_ModuleSagas_ModuleState
ON ModuleSagas
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ModuleState (ModuleId, OrganizationId, IsRunning, CurrentModuleJobId, LatestModuleJobId, LatestActualStateHeadline, DesiredStateHeadline, QueuedDesiredStateHeadline)
    SELECT i.CorrelationId, i.OrganizationId, 0, NULL, NULL, NULL, i.DesiredStateHeadline, i.QueuedDesiredStateHeadline
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
-- 12. Trigger on Modules to clean up ModuleState on module deletion
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
    INSERT INTO ModuleState (ModuleId, OrganizationId, IsRunning, CurrentModuleJobId, LatestModuleJobId, LatestActualStateHeadline, DesiredStateHeadline, QueuedDesiredStateHeadline)
    SELECT i.Id, i.OrganizationId, CAST(0 AS BIT), NULL, NULL, NULL, NULL, NULL
    FROM inserted i
    WHERE NOT EXISTS (SELECT 1 FROM ModuleState ms WHERE ms.ModuleId = i.Id);
END;
GO

-- ============================================================================
-- Backfill. The columns start NULL on every existing row, and the triggers only reach a Module
-- when something touches its jobs or saga, so without this a Module sits with no ids until it
-- next runs. The procedure truncates and rebuilds from the live rows, which is also the repair
-- for anything left wrong.
-- ============================================================================

EXEC sp_RecomputeModuleState;
GO

-- ============================================================================
-- The dependency graph is read through LINQ in DependencyGraphQueries, so the views it used
-- to read are gone. vw_DependencyEdges went with them: it only ever fed vw_Dependencies.
-- ============================================================================

DROP VIEW IF EXISTS vw_Dependencies;
GO

DROP VIEW IF EXISTS vw_RecursiveApplyDependencies;
GO

DROP VIEW IF EXISTS vw_RecursiveDestroyDependencies;
GO

DROP VIEW IF EXISTS vw_DependencyEdges;
GO
