-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- ==========================================================================
-- ModuleResolvedSettings: what each inheritable setting resolves to for one
-- Module, kept current by the triggers below.
--
-- A Module and the Namespaces above it form a chain, the Module first. For
-- one setting:
--   ForbidDepth  the furthest Namespace that forbids an override and sets a value
--   Base         that Namespace's value, or the nearest set value where none forbids
--   Floor        the strictest OnlyStricter value above ForbidDepth
-- and the answer is whichever of Base and Floor is stricter. The Module is the
-- nearest link and carries no pin, so a pin above it binds it as it binds a
-- Namespace.
--
-- Every value is compared as an int, higher being stricter. Engine and the two
-- durations have no order and so no OnlyStricter pin; their int is only a code.
-- ==========================================================================

SET QUOTED_IDENTIFIER ON;
GO

DROP VIEW IF EXISTS dbo.NamespaceResolvedDefaults;
GO

-- ==========================================================================
-- NamespaceAncestors is reconciled, not rewritten: only the rows that differ
-- from what the tree now implies are deleted or inserted, so what reads its
-- changes sees a change only where there was one.
-- ==========================================================================

CREATE OR ALTER PROCEDURE dbo.usp_RebuildNamespaceAncestors
    @OrganizationId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    CREATE TABLE #Desired (
        NamespaceId uniqueidentifier NOT NULL,
        AncestorNamespaceId uniqueidentifier NOT NULL,
        Depth int NOT NULL,
        PRIMARY KEY (NamespaceId, AncestorNamespaceId));

    -- Depth is capped so a cycle cannot recurse without end. NamespaceRepository refuses to
    -- write one; a cycle written past it is bounded here.
    ;WITH Ancestry AS
    (
        SELECT n.Id AS NamespaceId, n.Id AS AncestorNamespaceId, 0 AS Depth
        FROM dbo.Namespaces n
        WHERE n.OrganizationId = @OrganizationId

        UNION ALL

        SELECT a.NamespaceId, p.Id AS AncestorNamespaceId, a.Depth + 1
        FROM Ancestry a
        INNER JOIN dbo.Namespaces c
            ON c.Id = a.AncestorNamespaceId AND c.OrganizationId = @OrganizationId
        INNER JOIN dbo.Namespaces p
            ON p.Id = c.ParentNamespaceId AND p.OrganizationId = c.OrganizationId
        WHERE a.Depth < 20
    )
    INSERT INTO #Desired (NamespaceId, AncestorNamespaceId, Depth)
    SELECT NamespaceId, AncestorNamespaceId, MIN(Depth)
    FROM Ancestry
    GROUP BY NamespaceId, AncestorNamespaceId
    OPTION (MAXRECURSION 0);

    DELETE a
    FROM dbo.NamespaceAncestors a
    WHERE a.OrganizationId = @OrganizationId
      AND NOT EXISTS (SELECT 1 FROM #Desired d
                      WHERE d.NamespaceId = a.NamespaceId
                        AND d.AncestorNamespaceId = a.AncestorNamespaceId
                        AND d.Depth = a.Depth);

    INSERT INTO dbo.NamespaceAncestors (NamespaceId, AncestorNamespaceId, OrganizationId, Depth)
    SELECT d.NamespaceId, d.AncestorNamespaceId, @OrganizationId, d.Depth
    FROM #Desired d
    WHERE NOT EXISTS (SELECT 1 FROM dbo.NamespaceAncestors a
                      WHERE a.NamespaceId = d.NamespaceId
                        AND a.AncestorNamespaceId = d.AncestorNamespaceId
                        AND a.OrganizationId = @OrganizationId);
END
GO

-- An update reaches the closure only where it moved a Namespace.
CREATE OR ALTER TRIGGER dbo.trg_Namespaces_AncestorRebuild
ON dbo.Namespaces
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OrgIds TABLE (OrganizationId uniqueidentifier PRIMARY KEY);

    IF NOT EXISTS (SELECT 1 FROM inserted) OR NOT EXISTS (SELECT 1 FROM deleted)
        INSERT INTO @OrgIds (OrganizationId)
        SELECT OrganizationId FROM inserted
        UNION
        SELECT OrganizationId FROM deleted;
    ELSE IF UPDATE(ParentNamespaceId)
        INSERT INTO @OrgIds (OrganizationId)
        SELECT DISTINCT i.OrganizationId
        FROM inserted i
        INNER JOIN deleted d ON d.Id = i.Id AND d.OrganizationId = i.OrganizationId
        WHERE NOT EXISTS (SELECT i.ParentNamespaceId INTERSECT SELECT d.ParentNamespaceId);

    DECLARE @OrgId uniqueidentifier;
    DECLARE ns_org_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT OrganizationId FROM @OrgIds;

    OPEN ns_org_cursor;
    FETCH NEXT FROM ns_org_cursor INTO @OrgId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC dbo.usp_RebuildNamespaceAncestors @OrganizationId = @OrgId;
        FETCH NEXT FROM ns_org_cursor INTO @OrgId;
    END
    CLOSE ns_org_cursor;
    DEALLOCATE ns_org_cursor;
END
GO

-- Altering a trigger drops its order, and the closure has to be current before anything reads it.
EXEC sp_settriggerorder @triggername = 'dbo.trg_Namespaces_AncestorRebuild', @order = 'First', @stmttype = 'INSERT';
EXEC sp_settriggerorder @triggername = 'dbo.trg_Namespaces_AncestorRebuild', @order = 'First', @stmttype = 'UPDATE';
EXEC sp_settriggerorder @triggername = 'dbo.trg_Namespaces_AncestorRebuild', @order = 'First', @stmttype = 'DELETE';
GO

-- @ModuleIds empty means every Module.
CREATE OR ALTER PROCEDURE dbo.usp_RecomputeModuleResolvedSettings
    @ModuleIds dbo.GuidList READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @all bit = CASE WHEN EXISTS (SELECT 1 FROM @ModuleIds) THEN 0 ELSE 1 END;

    CREATE TABLE #Targets (
        ModuleId uniqueidentifier NOT NULL PRIMARY KEY,
        OrganizationId uniqueidentifier NOT NULL,
        NamespaceId uniqueidentifier NOT NULL);

    INSERT INTO #Targets (ModuleId, OrganizationId, NamespaceId)
    SELECT m.Id, m.OrganizationId, m.NamespaceId
    FROM dbo.Modules m
    WHERE @all = 1 OR EXISTS (SELECT 1 FROM @ModuleIds i WHERE i.Id = m.Id);

    IF NOT EXISTS (SELECT 1 FROM #Targets) RETURN;

    -- A row depends on its Module and on the chain above it, all under one root Namespace, so
    -- recomputes are serialized per root. One that spans many roots takes the whole table instead.
    IF @@TRANCOUNT > 0
    BEGIN
        DECLARE @roots TABLE (RootId uniqueidentifier NOT NULL PRIMARY KEY);
        DECLARE @rc int, @resource nvarchar(255), @rootId uniqueidentifier;

        IF @all = 0
            INSERT INTO @roots (RootId)
            SELECT DISTINCT a.AncestorNamespaceId
            FROM #Targets t
            INNER JOIN dbo.NamespaceAncestors a
                ON a.NamespaceId = t.NamespaceId AND a.OrganizationId = t.OrganizationId
            INNER JOIN dbo.Namespaces r
                ON r.Id = a.AncestorNamespaceId AND r.OrganizationId = a.OrganizationId
            WHERE r.ParentNamespaceId IS NULL;

        DECLARE @whole bit = CASE WHEN @all = 1 OR (SELECT COUNT(*) FROM @roots) > 50 THEN 1 ELSE 0 END;
        DECLARE @mode nvarchar(32) = CASE WHEN @whole = 1 THEN 'Exclusive' ELSE 'Shared' END;

        EXEC @rc = sp_getapplock @Resource = 'ModuleResolvedSettings', @LockMode = @mode,
                                 @LockOwner = 'Transaction', @LockTimeout = 30000;
        IF @rc < 0 THROW 51000, 'Timed out acquiring the resolved settings lock.', 1;

        IF @whole = 0
        BEGIN
            DECLARE root_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT RootId FROM @roots ORDER BY RootId;
            OPEN root_cursor;
            FETCH NEXT FROM root_cursor INTO @rootId;
            WHILE @@FETCH_STATUS = 0
            BEGIN
                SET @resource = 'ModuleResolvedSettings:' + CAST(@rootId AS char(36));
                EXEC @rc = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive',
                                         @LockOwner = 'Transaction', @LockTimeout = 30000;
                IF @rc < 0 THROW 51000, 'Timed out acquiring the resolved settings lock.', 1;
                FETCH NEXT FROM root_cursor INTO @rootId;
            END
            CLOSE root_cursor;
            DEALLOCATE root_cursor;
        END
    END

    -- One row per Module, setting and link of the chain that sets a value. The Module is depth -1.
    CREATE TABLE #Chain (
        ModuleId uniqueidentifier NOT NULL,
        Setting tinyint NOT NULL,
        Depth int NOT NULL,
        NamespaceId uniqueidentifier NOT NULL,
        Value int NOT NULL,
        Policy nvarchar(50) NULL,
        PRIMARY KEY (ModuleId, Setting, Depth));

    INSERT INTO #Chain (ModuleId, Setting, Depth, NamespaceId, Value, Policy)
    SELECT t.ModuleId, v.Setting, a.Depth, n.Id, v.Value, v.Policy
    FROM #Targets t
    INNER JOIN dbo.NamespaceAncestors a
        ON a.NamespaceId = t.NamespaceId AND a.OrganizationId = t.OrganizationId
    INNER JOIN dbo.Namespaces n
        ON n.Id = a.AncestorNamespaceId AND n.OrganizationId = a.OrganizationId
    CROSS APPLY (VALUES
            (1, CAST(n.DefaultCleanInitEnabled AS int), n.OverrideOnCleanInitEnabled),
            (2, CAST(n.DefaultTriggerPathFilterEnabled AS int), n.OverrideOnTriggerPathFilterEnabled),
            (3, n.DefaultApplyApprovalThreshold, n.OverrideOnApplyApprovalThreshold),
            (4, n.DefaultDestroyApprovalThreshold, n.OverrideOnDestroyApprovalThreshold),
            (5, n.DefaultStateMigrationApprovalThreshold, n.OverrideOnStateMigrationApprovalThreshold),
            (6, n.DefaultApprovalTimeoutMinutes, n.OverrideOnApprovalTimeoutMinutes),
            (7, CASE n.DefaultEngine WHEN 'OpenTofu' THEN 0 WHEN 'Terraform' THEN 1 WHEN 'Pulumi' THEN 2 END, n.OverrideOnEngine),
            (8, CAST(n.DefaultDriftCheckEnabled AS int), n.OverrideOnDriftCheckEnabled),
            (9, n.DefaultDriftCheckIntervalMinutes, n.OverrideOnDriftCheckIntervalMinutes),
            (10, CASE n.DefaultOutputSetDownstreamRestriction WHEN 'SensitiveOnly' THEN 0 WHEN 'All' THEN 1 END, n.OverrideOnOutputSetDownstreamRestriction),
            (11, CASE n.DefaultOutputRestrictionExemption WHEN 'OwnNamespaceAndDescendants' THEN 0 WHEN 'OwnNamespace' THEN 1 WHEN 'Nobody' THEN 2 END, n.OverrideOnOutputRestrictionExemption)
        ) v (Setting, Value, Policy)
    WHERE v.Value IS NOT NULL;

    INSERT INTO #Chain (ModuleId, Setting, Depth, NamespaceId, Value, Policy)
    SELECT t.ModuleId, v.Setting, -1, t.NamespaceId, v.Value, NULL
    FROM #Targets t
    INNER JOIN dbo.Modules m
        ON m.Id = t.ModuleId AND m.OrganizationId = t.OrganizationId
    CROSS APPLY (VALUES
            (1, CAST(m.CleanInitEnabled AS int)),
            (2, CAST(m.TriggerPathFilterEnabled AS int)),
            (3, m.ApplyApprovalThreshold),
            (4, m.DestroyApprovalThreshold),
            (5, m.StateMigrationApprovalThreshold),
            (6, m.ApprovalTimeoutMinutes),
            (7, CASE m.Engine WHEN 'OpenTofu' THEN 0 WHEN 'Terraform' THEN 1 WHEN 'Pulumi' THEN 2 END),
            (8, CAST(m.DriftCheckEnabled AS int)),
            (9, m.DriftCheckIntervalMinutes),
            (10, CASE m.OutputSetDownstreamRestriction WHEN 'SensitiveOnly' THEN 0 WHEN 'All' THEN 1 END),
            (11, CASE m.OutputRestrictionExemption WHEN 'OwnNamespaceAndDescendants' THEN 0 WHEN 'OwnNamespace' THEN 1 WHEN 'Nobody' THEN 2 END)
        ) v (Setting, Value)
    WHERE v.Value IS NOT NULL;

    SELECT ModuleId, Setting,
        MAX(CASE WHEN Policy = 'Forbidden' THEN Depth END) AS ForbidDepth,
        MIN(Depth) AS NearestDepth
    INTO #Marks
    FROM #Chain
    GROUP BY ModuleId, Setting;

    SELECT c.ModuleId, c.Setting, k.ForbidDepth,
        COALESCE(k.ForbidDepth, k.NearestDepth) AS BaseDepth,
        MAX(CASE WHEN c.Depth = COALESCE(k.ForbidDepth, k.NearestDepth) THEN c.Value END) AS Base,
        MAX(CASE WHEN c.Policy = 'OnlyStricter' AND c.Depth > ISNULL(k.ForbidDepth, -2) THEN c.Value END) AS Floor
    INTO #Folded
    FROM #Chain c
    INNER JOIN #Marks k ON k.ModuleId = c.ModuleId AND k.Setting = c.Setting
    GROUP BY c.ModuleId, c.Setting, k.ForbidDepth, k.NearestDepth;

    -- Where the exemption came from: the link that supplied Base, or the nearest one that set the Floor where the Floor won.
    SELECT f.ModuleId, c.NamespaceId
    INTO #ExemptionSource
    FROM #Folded f
    INNER JOIN #Chain c
        ON c.ModuleId = f.ModuleId AND c.Setting = f.Setting
        AND c.Depth = CASE WHEN f.Floor > f.Base
            THEN (SELECT MIN(x.Depth) FROM #Chain x
                  WHERE x.ModuleId = f.ModuleId AND x.Setting = f.Setting
                    AND x.Policy = 'OnlyStricter' AND x.Depth > ISNULL(f.ForbidDepth, -2) AND x.Value = f.Floor)
            ELSE f.BaseDepth END
    WHERE f.Setting = 11;

    SELECT t.ModuleId, t.OrganizationId,
        CAST(MAX(CASE WHEN r.Setting = 1 THEN r.Value END) AS bit) AS CleanInitEnabled,
        CAST(MAX(CASE WHEN r.Setting = 2 THEN r.Value END) AS bit) AS TriggerPathFilterEnabled,
        MAX(CASE WHEN r.Setting = 3 THEN r.Value END) AS ApplyApprovalThreshold,
        MAX(CASE WHEN r.Setting = 4 THEN r.Value END) AS DestroyApprovalThreshold,
        MAX(CASE WHEN r.Setting = 5 THEN r.Value END) AS StateMigrationApprovalThreshold,
        MAX(CASE WHEN r.Setting = 6 THEN r.Value END) AS ApprovalTimeoutMinutes,
        CASE MAX(CASE WHEN r.Setting = 7 THEN r.Value END) WHEN 0 THEN 'OpenTofu' WHEN 1 THEN 'Terraform' WHEN 2 THEN 'Pulumi' END AS Engine,
        CAST(MAX(CASE WHEN r.Setting = 8 THEN r.Value END) AS bit) AS DriftCheckEnabled,
        MAX(CASE WHEN r.Setting = 9 THEN r.Value END) AS DriftCheckIntervalMinutes,
        CASE MAX(CASE WHEN r.Setting = 10 THEN r.Value END) WHEN 0 THEN 'SensitiveOnly' WHEN 1 THEN 'All' END AS OutputSetDownstreamRestriction,
        CASE MAX(CASE WHEN r.Setting = 11 THEN r.Value END) WHEN 0 THEN 'OwnNamespaceAndDescendants' WHEN 1 THEN 'OwnNamespace' WHEN 2 THEN 'Nobody' END AS OutputRestrictionExemption,
        MAX(CAST(s.NamespaceId AS char(36))) AS ExemptionNamespaceId
    INTO #Resolved
    FROM #Targets t
    LEFT JOIN (SELECT ModuleId, Setting, CASE WHEN Floor > Base THEN Floor ELSE Base END AS Value FROM #Folded) r
        ON r.ModuleId = t.ModuleId
    LEFT JOIN #ExemptionSource s ON s.ModuleId = t.ModuleId
    GROUP BY t.ModuleId, t.OrganizationId;

    UPDATE x SET
        CleanInitEnabled = p.CleanInitEnabled,
        TriggerPathFilterEnabled = p.TriggerPathFilterEnabled,
        ApplyApprovalThreshold = p.ApplyApprovalThreshold,
        DestroyApprovalThreshold = p.DestroyApprovalThreshold,
        StateMigrationApprovalThreshold = p.StateMigrationApprovalThreshold,
        ApprovalTimeoutMinutes = p.ApprovalTimeoutMinutes,
        Engine = p.Engine,
        DriftCheckEnabled = p.DriftCheckEnabled,
        DriftCheckIntervalMinutes = p.DriftCheckIntervalMinutes,
        OutputSetDownstreamRestriction = p.OutputSetDownstreamRestriction,
        OutputRestrictionExemption = p.OutputRestrictionExemption,
        OutputRestrictionExemptionNamespaceId = CAST(p.ExemptionNamespaceId AS uniqueidentifier)
    FROM dbo.ModuleResolvedSettings x
    INNER JOIN #Resolved p ON p.ModuleId = x.ModuleId AND p.OrganizationId = x.OrganizationId
    WHERE NOT EXISTS (SELECT x.CleanInitEnabled, x.TriggerPathFilterEnabled, x.ApplyApprovalThreshold, x.DestroyApprovalThreshold, x.StateMigrationApprovalThreshold, x.ApprovalTimeoutMinutes, x.Engine, x.DriftCheckEnabled, x.DriftCheckIntervalMinutes, x.OutputSetDownstreamRestriction, x.OutputRestrictionExemption, x.OutputRestrictionExemptionNamespaceId
                      INTERSECT
                      SELECT p.CleanInitEnabled, p.TriggerPathFilterEnabled, p.ApplyApprovalThreshold, p.DestroyApprovalThreshold, p.StateMigrationApprovalThreshold, p.ApprovalTimeoutMinutes, p.Engine, p.DriftCheckEnabled, p.DriftCheckIntervalMinutes, p.OutputSetDownstreamRestriction, p.OutputRestrictionExemption, CAST(p.ExemptionNamespaceId AS uniqueidentifier));

    INSERT INTO dbo.ModuleResolvedSettings (ModuleId, OrganizationId, CleanInitEnabled, TriggerPathFilterEnabled, ApplyApprovalThreshold, DestroyApprovalThreshold, StateMigrationApprovalThreshold, ApprovalTimeoutMinutes, Engine, DriftCheckEnabled, DriftCheckIntervalMinutes, OutputSetDownstreamRestriction, OutputRestrictionExemption, OutputRestrictionExemptionNamespaceId)
    SELECT p.ModuleId, p.OrganizationId, p.CleanInitEnabled, p.TriggerPathFilterEnabled, p.ApplyApprovalThreshold, p.DestroyApprovalThreshold, p.StateMigrationApprovalThreshold, p.ApprovalTimeoutMinutes, p.Engine, p.DriftCheckEnabled, p.DriftCheckIntervalMinutes, p.OutputSetDownstreamRestriction, p.OutputRestrictionExemption, CAST(p.ExemptionNamespaceId AS uniqueidentifier)
    FROM #Resolved p
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ModuleResolvedSettings x
                      WHERE x.ModuleId = p.ModuleId AND x.OrganizationId = p.OrganizationId);
END
GO

-- A Module's own values, or the Namespace it sits in.
CREATE OR ALTER TRIGGER dbo.trg_Modules_ResolvedSettings
ON dbo.Modules
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM deleted) AND NOT (UPDATE(NamespaceId) OR UPDATE(CleanInitEnabled) OR UPDATE(TriggerPathFilterEnabled) OR UPDATE(ApplyApprovalThreshold) OR UPDATE(DestroyApprovalThreshold) OR UPDATE(StateMigrationApprovalThreshold) OR UPDATE(ApprovalTimeoutMinutes) OR UPDATE(Engine) OR UPDATE(DriftCheckEnabled) OR UPDATE(DriftCheckIntervalMinutes) OR UPDATE(OutputSetDownstreamRestriction) OR UPDATE(OutputRestrictionExemption))
        RETURN;

    DECLARE @moduleIds dbo.GuidList;

    INSERT INTO @moduleIds (Id)
    SELECT i.Id
    FROM inserted i
    LEFT JOIN deleted d ON d.Id = i.Id
    WHERE d.Id IS NULL
       OR NOT EXISTS (SELECT i.NamespaceId, i.CleanInitEnabled, i.TriggerPathFilterEnabled, i.ApplyApprovalThreshold, i.DestroyApprovalThreshold, i.StateMigrationApprovalThreshold, i.ApprovalTimeoutMinutes, i.Engine, i.DriftCheckEnabled, i.DriftCheckIntervalMinutes, i.OutputSetDownstreamRestriction, i.OutputRestrictionExemption
                      INTERSECT
                      SELECT d.NamespaceId, d.CleanInitEnabled, d.TriggerPathFilterEnabled, d.ApplyApprovalThreshold, d.DestroyApprovalThreshold, d.StateMigrationApprovalThreshold, d.ApprovalTimeoutMinutes, d.Engine, d.DriftCheckEnabled, d.DriftCheckIntervalMinutes, d.OutputSetDownstreamRestriction, d.OutputRestrictionExemption);

    IF EXISTS (SELECT 1 FROM @moduleIds)
        EXEC dbo.usp_RecomputeModuleResolvedSettings @ModuleIds = @moduleIds;
END
GO

-- A default or a pin on a Namespace, which reaches every Module in it and below it.
CREATE OR ALTER TRIGGER dbo.trg_Namespaces_ResolvedSettings
ON dbo.Namespaces
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT (UPDATE(DefaultCleanInitEnabled) OR UPDATE(DefaultTriggerPathFilterEnabled) OR UPDATE(DefaultApplyApprovalThreshold) OR UPDATE(DefaultDestroyApprovalThreshold) OR UPDATE(DefaultStateMigrationApprovalThreshold) OR UPDATE(DefaultApprovalTimeoutMinutes) OR UPDATE(DefaultEngine) OR UPDATE(DefaultDriftCheckEnabled) OR UPDATE(DefaultDriftCheckIntervalMinutes) OR UPDATE(DefaultOutputSetDownstreamRestriction) OR UPDATE(DefaultOutputRestrictionExemption) OR UPDATE(OverrideOnCleanInitEnabled) OR UPDATE(OverrideOnTriggerPathFilterEnabled) OR UPDATE(OverrideOnApplyApprovalThreshold) OR UPDATE(OverrideOnDestroyApprovalThreshold) OR UPDATE(OverrideOnStateMigrationApprovalThreshold) OR UPDATE(OverrideOnApprovalTimeoutMinutes) OR UPDATE(OverrideOnEngine) OR UPDATE(OverrideOnDriftCheckEnabled) OR UPDATE(OverrideOnDriftCheckIntervalMinutes) OR UPDATE(OverrideOnOutputSetDownstreamRestriction) OR UPDATE(OverrideOnOutputRestrictionExemption))
        RETURN;

    DECLARE @moduleIds dbo.GuidList;

    INSERT INTO @moduleIds (Id)
    SELECT DISTINCT m.Id
    FROM inserted i
    INNER JOIN deleted d ON d.Id = i.Id
    INNER JOIN dbo.NamespaceAncestors a
        ON a.AncestorNamespaceId = i.Id AND a.OrganizationId = i.OrganizationId
    INNER JOIN dbo.Modules m
        ON m.NamespaceId = a.NamespaceId AND m.OrganizationId = a.OrganizationId
    WHERE NOT EXISTS (SELECT i.DefaultCleanInitEnabled, i.DefaultTriggerPathFilterEnabled, i.DefaultApplyApprovalThreshold, i.DefaultDestroyApprovalThreshold, i.DefaultStateMigrationApprovalThreshold, i.DefaultApprovalTimeoutMinutes, i.DefaultEngine, i.DefaultDriftCheckEnabled, i.DefaultDriftCheckIntervalMinutes, i.DefaultOutputSetDownstreamRestriction, i.DefaultOutputRestrictionExemption, i.OverrideOnCleanInitEnabled, i.OverrideOnTriggerPathFilterEnabled, i.OverrideOnApplyApprovalThreshold, i.OverrideOnDestroyApprovalThreshold, i.OverrideOnStateMigrationApprovalThreshold, i.OverrideOnApprovalTimeoutMinutes, i.OverrideOnEngine, i.OverrideOnDriftCheckEnabled, i.OverrideOnDriftCheckIntervalMinutes, i.OverrideOnOutputSetDownstreamRestriction, i.OverrideOnOutputRestrictionExemption
                      INTERSECT
                      SELECT d.DefaultCleanInitEnabled, d.DefaultTriggerPathFilterEnabled, d.DefaultApplyApprovalThreshold, d.DefaultDestroyApprovalThreshold, d.DefaultStateMigrationApprovalThreshold, d.DefaultApprovalTimeoutMinutes, d.DefaultEngine, d.DefaultDriftCheckEnabled, d.DefaultDriftCheckIntervalMinutes, d.DefaultOutputSetDownstreamRestriction, d.DefaultOutputRestrictionExemption, d.OverrideOnCleanInitEnabled, d.OverrideOnTriggerPathFilterEnabled, d.OverrideOnApplyApprovalThreshold, d.OverrideOnDestroyApprovalThreshold, d.OverrideOnStateMigrationApprovalThreshold, d.OverrideOnApprovalTimeoutMinutes, d.OverrideOnEngine, d.OverrideOnDriftCheckEnabled, d.OverrideOnDriftCheckIntervalMinutes, d.OverrideOnOutputSetDownstreamRestriction, d.OverrideOnOutputRestrictionExemption);

    IF EXISTS (SELECT 1 FROM @moduleIds)
        EXEC dbo.usp_RecomputeModuleResolvedSettings @ModuleIds = @moduleIds;
END
GO

-- A changed chain: a Namespace created under another, moved, or removed. The closure is reconciled, so these are real changes.
CREATE OR ALTER TRIGGER dbo.trg_NamespaceAncestors_ResolvedSettings
ON dbo.NamespaceAncestors
AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @moduleIds dbo.GuidList;

    INSERT INTO @moduleIds (Id)
    SELECT DISTINCT m.Id
    FROM (SELECT NamespaceId, OrganizationId FROM inserted
          UNION
          SELECT NamespaceId, OrganizationId FROM deleted) c
    INNER JOIN dbo.Modules m
        ON m.NamespaceId = c.NamespaceId AND m.OrganizationId = c.OrganizationId;

    IF EXISTS (SELECT 1 FROM @moduleIds)
        EXEC dbo.usp_RecomputeModuleResolvedSettings @ModuleIds = @moduleIds;
END
GO

DECLARE @everyModule dbo.GuidList;
EXEC dbo.usp_RecomputeModuleResolvedSettings @ModuleIds = @everyModule;
GO
