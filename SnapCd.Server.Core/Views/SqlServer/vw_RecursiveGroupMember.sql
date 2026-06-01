-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

CREATE OR ALTER VIEW [dbo].[vw_RecursiveGroupMember] AS
WITH RecursiveGroupOrganizationUser AS (
    -- Anchor 1: Each group is a member of itself (depth 0)
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

    UNION ALL

    -- Anchor 2: Direct group organizationUsers (depth 1)
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

    UNION ALL

    -- Recursive step: Follow parent group organizationUsers
    SELECT
        -- Root group details (always preserved)
        r.RootGroupId,
        r.RootOrganizationId,
        r.RootGroupName,

        -- Current parent group
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
    -- Limit recursion depth (cycles will create duplicates but are bounded)
    WHERE r.Depth < 10
        AND ggm.GroupMemberDiscriminator = 'Group'
)
SELECT
    RootGroupId,
    RootOrganizationId,
    RootGroupName,
    GroupId,
    OrganizationId,
    GroupName,
    Depth,
    VisitedPath
FROM RecursiveGroupOrganizationUser;
