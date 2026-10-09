-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- ==========================================================================
-- The RecursiveGroupMembers rebuild procedure and triggers as they stood
-- before this migration, for its Down.
--
-- Rolling back by dropping them would leave the closure with nothing
-- maintaining it: group membership changes would stop updating it and the
-- permission queries that read it would go quiet rather than fail.
--
-- The trigger order is reset to unordered, which is how it stood before.
-- ==========================================================================

SET QUOTED_IDENTIFIER ON;
GO

-- Recompute rows for a single organization.
CREATE OR ALTER PROCEDURE dbo.usp_RebuildRecursiveGroupMembers
    @OrganizationId uniqueidentifier
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.RecursiveGroupMembers
    WHERE RootOrganizationId = @OrganizationId;

    ;WITH RecursiveGroupOrganizationUser AS (
        SELECT
            g.Id AS RootGroupId,
            g.OrganizationId AS RootOrganizationId,
            g.Name AS RootGroupName,
            g.Id AS GroupId,
            g.OrganizationId,
            g.Name AS GroupName,
            0 AS Depth,
            CAST('|' + CAST(g.Id AS VARCHAR(36)) + '|' AS NVARCHAR(MAX)) AS VisitedPath
        FROM dbo.Groups g
        WHERE g.OrganizationId = @OrganizationId

        UNION ALL

        SELECT
            ggm.MemberGroupId AS RootGroupId,
            ggm.OrganizationId AS RootOrganizationId,
            mg.Name AS RootGroupName,
            ggm.GroupId,
            ggm.OrganizationId,
            pg.Name AS GroupName,
            1 AS Depth,
            CAST('|' + CAST(ggm.MemberGroupId AS VARCHAR(36)) + '|' + CAST(ggm.GroupId AS VARCHAR(36)) + '|' AS NVARCHAR(MAX)) AS VisitedPath
        FROM dbo.GroupMembers ggm
        INNER JOIN dbo.Groups mg ON ggm.MemberGroupId = mg.Id AND ggm.OrganizationId = mg.OrganizationId
        INNER JOIN dbo.Groups pg ON ggm.GroupId = pg.Id AND ggm.OrganizationId = pg.OrganizationId
        WHERE ggm.GroupMemberDiscriminator = 'Group'
            AND ggm.OrganizationId = @OrganizationId

        UNION ALL

        SELECT
            r.RootGroupId,
            r.RootOrganizationId,
            r.RootGroupName,
            ggm.GroupId,
            ggm.OrganizationId,
            pg.Name AS GroupName,
            r.Depth + 1,
            CAST(r.VisitedPath + CAST(ggm.GroupId AS VARCHAR(36)) + '|' AS NVARCHAR(MAX))
        FROM RecursiveGroupOrganizationUser r
        INNER JOIN dbo.GroupMembers ggm
            ON r.GroupId = ggm.MemberGroupId
            AND r.OrganizationId = ggm.OrganizationId
        INNER JOIN dbo.Groups pg
            ON ggm.GroupId = pg.Id
            AND ggm.OrganizationId = pg.OrganizationId
        WHERE r.Depth < 10
            AND ggm.GroupMemberDiscriminator = 'Group'
    )
    INSERT INTO dbo.RecursiveGroupMembers
        (RootGroupId, RootOrganizationId, RootGroupName, GroupId, OrganizationId, GroupName, Depth)
    SELECT
        RootGroupId, RootOrganizationId, RootGroupName, GroupId, OrganizationId, GroupName, Depth
    FROM (
        SELECT *,
            ROW_NUMBER() OVER (
                PARTITION BY RootGroupId, RootOrganizationId, GroupId, OrganizationId
                ORDER BY Depth ASC
            ) AS rn
        FROM RecursiveGroupOrganizationUser
    ) deduped
    WHERE rn = 1;
END
GO

-- Rebuild on GroupMembers / Groups changes.
CREATE OR ALTER TRIGGER dbo.trg_GroupMembers_RecursiveRebuild
ON dbo.GroupMembers
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OrgIds TABLE (OrganizationId uniqueidentifier);

    INSERT INTO @OrgIds (OrganizationId)
    SELECT DISTINCT OrganizationId FROM inserted
    UNION
    SELECT DISTINCT OrganizationId FROM deleted;

    DECLARE @OrgId uniqueidentifier;
    DECLARE org_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT OrganizationId FROM @OrgIds;

    OPEN org_cursor;
    FETCH NEXT FROM org_cursor INTO @OrgId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC dbo.usp_RebuildRecursiveGroupMembers @OrganizationId = @OrgId;
        FETCH NEXT FROM org_cursor INTO @OrgId;
    END
    CLOSE org_cursor;
    DEALLOCATE org_cursor;
END
GO

CREATE OR ALTER TRIGGER dbo.trg_Groups_RecursiveRebuild
ON dbo.Groups
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OrgIds TABLE (OrganizationId uniqueidentifier);

    INSERT INTO @OrgIds (OrganizationId)
    SELECT DISTINCT OrganizationId FROM inserted
    UNION
    SELECT DISTINCT OrganizationId FROM deleted;

    DECLARE @OrgId uniqueidentifier;
    DECLARE org_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT OrganizationId FROM @OrgIds;

    OPEN org_cursor;
    FETCH NEXT FROM org_cursor INTO @OrgId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC dbo.usp_RebuildRecursiveGroupMembers @OrganizationId = @OrgId;
        FETCH NEXT FROM org_cursor INTO @OrgId;
    END
    CLOSE org_cursor;
    DEALLOCATE org_cursor;
END
GO

EXEC sp_settriggerorder @triggername = 'dbo.trg_GroupMembers_RecursiveRebuild',
                        @order = 'None', @stmttype = 'INSERT';
EXEC sp_settriggerorder @triggername = 'dbo.trg_GroupMembers_RecursiveRebuild',
                        @order = 'None', @stmttype = 'UPDATE';
EXEC sp_settriggerorder @triggername = 'dbo.trg_GroupMembers_RecursiveRebuild',
                        @order = 'None', @stmttype = 'DELETE';
GO
