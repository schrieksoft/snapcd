-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- ==========================================================================
-- NamespaceAncestors maintenance: the rebuild procedure and the trigger that
-- calls it.
--
-- A Namespace resolves its defaults and its permissions from itself and every
-- Namespace above it, so the chain is materialised here rather than walked per
-- read. Each row pairs a Namespace with one ancestor; a Namespace is its own
-- ancestor at depth 0.
-- ==========================================================================

SET QUOTED_IDENTIFIER ON;
GO

-- Recompute rows for a single organization.
CREATE OR ALTER PROCEDURE dbo.usp_RebuildNamespaceAncestors
    @OrganizationId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.NamespaceAncestors
    WHERE OrganizationId = @OrganizationId;

    -- Depth is capped so a cycle written before the application check existed cannot
    -- recurse without end. A cycle is refused on write; this only bounds the damage.
    ;WITH Ancestry AS
    (
        SELECT
            n.Id AS NamespaceId,
            n.Id AS AncestorNamespaceId,
            n.OrganizationId,
            0 AS Depth
        FROM dbo.Namespaces n
        WHERE n.OrganizationId = @OrganizationId

        UNION ALL

        SELECT
            a.NamespaceId,
            p.Id AS AncestorNamespaceId,
            a.OrganizationId,
            a.Depth + 1
        FROM Ancestry a
        INNER JOIN dbo.Namespaces c
            ON c.Id = a.AncestorNamespaceId
            AND c.OrganizationId = a.OrganizationId
        INNER JOIN dbo.Namespaces p
            ON p.Id = c.ParentNamespaceId
            AND p.OrganizationId = c.OrganizationId
        WHERE a.Depth < 20
    )
    INSERT INTO dbo.NamespaceAncestors (NamespaceId, AncestorNamespaceId, OrganizationId, Depth)
    SELECT NamespaceId, AncestorNamespaceId, OrganizationId, Depth
    FROM (
        SELECT *,
            ROW_NUMBER() OVER (
                PARTITION BY NamespaceId, AncestorNamespaceId, OrganizationId
                ORDER BY Depth ASC
            ) AS rn
        FROM Ancestry
    ) deduped
    WHERE rn = 1
    OPTION (MAXRECURSION 0);
END
GO

-- Rebuild when a Namespace is added, removed, renamed or reparented.
CREATE OR ALTER TRIGGER dbo.trg_Namespaces_AncestorRebuild
ON dbo.Namespaces
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Only the parent link changes the shape. An ordinary edit leaves the closure alone.
    IF NOT EXISTS (SELECT 1 FROM inserted) OR NOT EXISTS (SELECT 1 FROM deleted)
        OR UPDATE(ParentNamespaceId)
    BEGIN
        DECLARE @OrgIds TABLE (OrganizationId uniqueidentifier PRIMARY KEY);

        INSERT INTO @OrgIds (OrganizationId)
        SELECT DISTINCT OrganizationId FROM inserted
        UNION
        SELECT DISTINCT OrganizationId FROM deleted;

        DECLARE @OrgId uniqueidentifier;
        DECLARE ns_org_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT OrganizationId FROM @OrgIds;

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
END
GO

-- Populate from the rows already present.
DECLARE @SeedOrgId uniqueidentifier;
DECLARE seed_org_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT DISTINCT Id FROM dbo.Organizations;

OPEN seed_org_cursor;
FETCH NEXT FROM seed_org_cursor INTO @SeedOrgId;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC dbo.usp_RebuildNamespaceAncestors @OrganizationId = @SeedOrgId;
    FETCH NEXT FROM seed_org_cursor INTO @SeedOrgId;
END
CLOSE seed_org_cursor;
DEALLOCATE seed_org_cursor;
GO

EXEC sp_settriggerorder @triggername = 'dbo.trg_Namespaces_AncestorRebuild',
                        @order = 'First', @stmttype = 'INSERT';
EXEC sp_settriggerorder @triggername = 'dbo.trg_Namespaces_AncestorRebuild',
                        @order = 'First', @stmttype = 'UPDATE';
EXEC sp_settriggerorder @triggername = 'dbo.trg_Namespaces_AncestorRebuild',
                        @order = 'First', @stmttype = 'DELETE';
GO
