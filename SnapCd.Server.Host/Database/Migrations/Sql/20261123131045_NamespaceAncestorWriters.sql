-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- ==========================================================================
-- NamespaceAncestors: who writes what, and what makes them take turns.
--
-- A writer touches only the rows of the Namespaces it is writing. A create
-- inserts the new Namespace's own rows, a delete removes them, and a move
-- recomputes the moved Namespace and everything below it. Two creates
-- therefore never write the same row.
--
-- A create copies the chain above its parent, which a move can change. One
-- application lock per organization keeps them apart: creates and deletes
-- take it shared and never wait for each other, a move and the repair take it
-- exclusive.
-- ==========================================================================

SET QUOTED_IDENTIFIER ON;
GO

-- The repair: brings an organization's rows back in line with its tree.
CREATE OR ALTER PROCEDURE dbo.usp_RebuildNamespaceAncestors
    @OrganizationId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    IF @@TRANCOUNT > 0
    BEGIN
        DECLARE @rc int, @resource nvarchar(255) = 'NamespaceAncestors:' + CAST(@OrganizationId AS char(36));
        EXEC @rc = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive',
                                 @LockOwner = 'Transaction', @LockTimeout = 30000;
        IF @rc < 0 THROW 51000, 'Timed out acquiring the namespace ancestry lock.', 1;
    END

    CREATE TABLE #Desired (
        NamespaceId uniqueidentifier NOT NULL,
        AncestorNamespaceId uniqueidentifier NOT NULL,
        Depth int NOT NULL,
        PRIMARY KEY (NamespaceId, AncestorNamespaceId));

    ;WITH Ancestry AS
    (
        SELECT n.Id AS NamespaceId, n.Id AS AncestorNamespaceId, 0 AS Depth
        FROM dbo.Namespaces n
        WHERE n.OrganizationId = @OrganizationId

        UNION ALL

        SELECT a.NamespaceId, c.ParentNamespaceId, a.Depth + 1
        FROM Ancestry a
        INNER JOIN dbo.Namespaces c
            ON c.Id = a.AncestorNamespaceId AND c.OrganizationId = @OrganizationId
        WHERE c.ParentNamespaceId IS NOT NULL AND a.Depth < 20
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

CREATE OR ALTER TRIGGER dbo.trg_Namespaces_AncestorRebuild
ON dbo.Namespaces
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @created bit = CASE WHEN EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted) THEN 1 ELSE 0 END;
    DECLARE @removed bit = CASE WHEN EXISTS (SELECT 1 FROM deleted) AND NOT EXISTS (SELECT 1 FROM inserted) THEN 1 ELSE 0 END;
    DECLARE @updated bit = CASE WHEN EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted) THEN 1 ELSE 0 END;

    IF @created = 0 AND @removed = 0 AND @updated = 0 RETURN;
    IF @updated = 1 AND NOT UPDATE(ParentNamespaceId) RETURN;

    -- The Namespaces whose own rows this statement decides: the new ones, or the ones that moved.
    DECLARE @written TABLE (Id uniqueidentifier NOT NULL PRIMARY KEY, OrganizationId uniqueidentifier NOT NULL);

    IF @created = 1
        INSERT INTO @written (Id, OrganizationId) SELECT Id, OrganizationId FROM inserted;
    ELSE IF @removed = 1
        INSERT INTO @written (Id, OrganizationId) SELECT Id, OrganizationId FROM deleted;
    ELSE
        INSERT INTO @written (Id, OrganizationId)
        SELECT i.Id, i.OrganizationId
        FROM inserted i
        INNER JOIN deleted d ON d.Id = i.Id AND d.OrganizationId = i.OrganizationId
        WHERE NOT EXISTS (SELECT i.ParentNamespaceId INTERSECT SELECT d.ParentNamespaceId);

    IF NOT EXISTS (SELECT 1 FROM @written) RETURN;

    DECLARE @mode nvarchar(32) = CASE WHEN @updated = 1 THEN 'Exclusive' ELSE 'Shared' END;
    DECLARE @rc int, @resource nvarchar(255), @organizationId uniqueidentifier;

    DECLARE org_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT DISTINCT OrganizationId FROM @written ORDER BY OrganizationId;
    OPEN org_cursor;
    FETCH NEXT FROM org_cursor INTO @organizationId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @resource = 'NamespaceAncestors:' + CAST(@organizationId AS char(36));
        EXEC @rc = sp_getapplock @Resource = @resource, @LockMode = @mode,
                                 @LockOwner = 'Transaction', @LockTimeout = 30000;
        IF @rc < 0 THROW 51000, 'Timed out acquiring the namespace ancestry lock.', 1;
        FETCH NEXT FROM org_cursor INTO @organizationId;
    END
    CLOSE org_cursor;
    DEALLOCATE org_cursor;

    IF @removed = 1
    BEGIN
        DELETE a
        FROM dbo.NamespaceAncestors a
        INNER JOIN @written w
            ON w.OrganizationId = a.OrganizationId
            AND (w.Id = a.NamespaceId OR w.Id = a.AncestorNamespaceId);
        RETURN;
    END

    -- A move takes everything below the moved Namespace with it. Read after the lock, so no other move is part-way through.
    DECLARE @targets TABLE (Id uniqueidentifier NOT NULL PRIMARY KEY, OrganizationId uniqueidentifier NOT NULL);

    INSERT INTO @targets (Id, OrganizationId)
    SELECT Id, OrganizationId FROM @written
    UNION
    SELECT a.NamespaceId, a.OrganizationId
    FROM @written w
    INNER JOIN dbo.NamespaceAncestors a
        ON a.AncestorNamespaceId = w.Id AND a.OrganizationId = w.OrganizationId
    WHERE @updated = 1;

    -- Each target's chain, read off the tree itself by walking up from it.
    CREATE TABLE #Walk (
        NamespaceId uniqueidentifier NOT NULL,
        AncestorNamespaceId uniqueidentifier NOT NULL,
        OrganizationId uniqueidentifier NOT NULL,
        Depth int NOT NULL);

    ;WITH Walk AS
    (
        SELECT t.Id AS NamespaceId, t.Id AS AncestorNamespaceId, t.OrganizationId, 0 AS Depth
        FROM @targets t

        UNION ALL

        SELECT w.NamespaceId, c.ParentNamespaceId, w.OrganizationId, w.Depth + 1
        FROM Walk w
        INNER JOIN dbo.Namespaces c
            ON c.Id = w.AncestorNamespaceId AND c.OrganizationId = w.OrganizationId
        WHERE c.ParentNamespaceId IS NOT NULL AND w.Depth < 21
    )
    INSERT INTO #Walk (NamespaceId, AncestorNamespaceId, OrganizationId, Depth)
    SELECT NamespaceId, AncestorNamespaceId, OrganizationId, Depth
    FROM Walk
    OPTION (MAXRECURSION 0);

    -- NamespaceRepository refuses both of these first. Two moves at once can each pass its check, and only here are they seen together.
    IF EXISTS (SELECT 1 FROM #Walk WHERE Depth > 0 AND AncestorNamespaceId = NamespaceId)
        THROW 51001, 'A Namespace cannot be placed under itself or under a Namespace inside it.', 1;

    IF EXISTS (SELECT 1 FROM #Walk WHERE Depth > 20)
        THROW 51002, 'Namespaces can be nested at most 20 levels deep.', 1;

    DELETE a
    FROM dbo.NamespaceAncestors a
    INNER JOIN @targets t ON t.Id = a.NamespaceId AND t.OrganizationId = a.OrganizationId
    WHERE NOT EXISTS (SELECT 1 FROM #Walk w
                      WHERE w.NamespaceId = a.NamespaceId
                        AND w.AncestorNamespaceId = a.AncestorNamespaceId
                        AND w.Depth = a.Depth);

    INSERT INTO dbo.NamespaceAncestors (NamespaceId, AncestorNamespaceId, OrganizationId, Depth)
    SELECT w.NamespaceId, w.AncestorNamespaceId, w.OrganizationId, w.Depth
    FROM #Walk w
    WHERE NOT EXISTS (SELECT 1 FROM dbo.NamespaceAncestors a
                      WHERE a.NamespaceId = w.NamespaceId
                        AND a.AncestorNamespaceId = w.AncestorNamespaceId
                        AND a.OrganizationId = w.OrganizationId);
END
GO

-- Altering a trigger drops its order, and the closure has to be current before anything reads it.
EXEC sp_settriggerorder @triggername = 'dbo.trg_Namespaces_AncestorRebuild', @order = 'First', @stmttype = 'INSERT';
EXEC sp_settriggerorder @triggername = 'dbo.trg_Namespaces_AncestorRebuild', @order = 'First', @stmttype = 'UPDATE';
EXEC sp_settriggerorder @triggername = 'dbo.trg_Namespaces_AncestorRebuild', @order = 'First', @stmttype = 'DELETE';
GO

-- Repairs any organization whose rows an earlier version of the procedure left out of line.
DECLARE @repairOrgId uniqueidentifier;
DECLARE repair_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT Id FROM dbo.Organizations;
OPEN repair_cursor;
FETCH NEXT FROM repair_cursor INTO @repairOrgId;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC dbo.usp_RebuildNamespaceAncestors @OrganizationId = @repairOrgId;
    FETCH NEXT FROM repair_cursor INTO @repairOrgId;
END
CLOSE repair_cursor;
DEALLOCATE repair_cursor;
GO
