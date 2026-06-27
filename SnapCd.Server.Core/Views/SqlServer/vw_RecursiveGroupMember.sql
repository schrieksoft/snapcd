-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- ==========================================================================
-- RecursiveGroupMembers: trigger-maintained materialization of the
-- recursive group membership CTE.
--
-- Triggers on GroupMembers and Groups rebuild the affected organization's
-- rows whenever group membership or group definitions change.
-- ==========================================================================

-- Step 1: Drop the legacy view if it exists (we now use the table directly)
IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_RecursiveGroupMember')
    DROP VIEW dbo.vw_RecursiveGroupMember;
GO

-- Step 2: Rename RecursiveGroupMembersPhysical to RecursiveGroupMembers if needed
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RecursiveGroupMembersPhysical')
    AND NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RecursiveGroupMembers')
BEGIN
    EXEC sp_rename 'dbo.RecursiveGroupMembersPhysical', 'RecursiveGroupMembers';
    EXEC sp_rename 'RecursiveGroupMembers.PK_RecursiveGroupMembersPhysical', 'PK_RecursiveGroupMembers', 'OBJECT';
END
GO

-- Step 3: Stored procedure to recompute rows for a single organization
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

-- Step 4: Triggers to rebuild on GroupMembers / Groups changes
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

-- Step 5: Populate from existing data (idempotent — rebuilds all orgs)
DECLARE @OrgId uniqueidentifier;
DECLARE org_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT DISTINCT Id FROM dbo.Organizations;

OPEN org_cursor;
FETCH NEXT FROM org_cursor INTO @OrgId;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC dbo.usp_RebuildRecursiveGroupMembers @OrganizationId = @OrgId;
    FETCH NEXT FROM org_cursor INTO @OrgId;
END
CLOSE org_cursor;
DEALLOCATE org_cursor;
GO
