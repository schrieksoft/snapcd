-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- ==========================================================================
-- DerivedOrganizationRoleAssignments: organization roles a principal holds
-- because of what they hold elsewhere, rather than because someone granted it.
--
-- So far one role is derived. IdentityAccessMetadataReader goes to anyone who
-- may grant a role somewhere, since choosing who to grant it to means seeing
-- the organization's principals. Asking that per read costs a seek per
-- reachable group per scope, so it is derived here instead.
--
-- It is a table of its own rather than rows in OrganizationRoleAssignments
-- because a trigger that writes to the table it fires on breaks the row
-- accounting EF relies on.
-- ==========================================================================

SET QUOTED_IDENTIFIER ON;
GO

-- Step 0: the principal set a trigger hands over.
IF NOT EXISTS (SELECT 1 FROM sys.types WHERE name = 'PrincipalScopeList' AND is_table_type = 1)
    CREATE TYPE dbo.PrincipalScopeList AS TABLE (
        PrincipalId uniqueidentifier NOT NULL,
        OrganizationId uniqueidentifier NOT NULL,
        PrincipalDiscriminator nvarchar(50) NOT NULL,
        PRIMARY KEY (PrincipalId, OrganizationId));
GO

-- Step 1: the principals a change affects, and whether each still qualifies.
CREATE OR ALTER PROCEDURE dbo.usp_SyncIdentityAccessMetadataReaders
    @Principals dbo.PrincipalScopeList READONLY
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM @Principals) RETURN;

    DECLARE @Qualifying TABLE (
        PrincipalId uniqueidentifier NOT NULL,
        OrganizationId uniqueidentifier NOT NULL,
        PrincipalDiscriminator nvarchar(50) NOT NULL,
        PRIMARY KEY (PrincipalId, OrganizationId));

    INSERT INTO @Qualifying (PrincipalId, OrganizationId, PrincipalDiscriminator)
    SELECT DISTINCT p.PrincipalId, p.OrganizationId, p.PrincipalDiscriminator
    FROM @Principals p
    WHERE EXISTS (
        -- directly on any scope
        SELECT 1 FROM dbo.OrganizationRoleAssignments r
        WHERE r.PrincipalId = p.PrincipalId AND r.OrganizationId = p.OrganizationId
          AND r.RoleName IN ('Owner', 'IdentityAccessManager')
        UNION ALL
        SELECT 1 FROM dbo.StackRoleAssignments r
        WHERE r.PrincipalId = p.PrincipalId AND r.OrganizationId = p.OrganizationId
          AND r.RoleName IN ('Owner', 'IdentityAccessManager')
        UNION ALL
        SELECT 1 FROM dbo.NamespaceRoleAssignments r
        WHERE r.PrincipalId = p.PrincipalId AND r.OrganizationId = p.OrganizationId
          AND r.RoleName IN ('Owner', 'IdentityAccessManager')
        UNION ALL
        SELECT 1 FROM dbo.ModuleRoleAssignments r
        WHERE r.PrincipalId = p.PrincipalId AND r.OrganizationId = p.OrganizationId
          AND r.RoleName IN ('Owner', 'IdentityAccessManager')
        UNION ALL
        SELECT 1 FROM dbo.RunnerRoleAssignments r
        WHERE r.PrincipalId = p.PrincipalId AND r.OrganizationId = p.OrganizationId
          AND r.RoleName IN ('Owner', 'IdentityAccessManager')
        UNION ALL
        SELECT 1 FROM dbo.AgentRoleAssignments r
        WHERE r.PrincipalId = p.PrincipalId AND r.OrganizationId = p.OrganizationId
          AND r.RoleName IN ('Owner', 'IdentityAccessManager')
        UNION ALL
        SELECT 1 FROM dbo.StateStoreRoleAssignments r
        WHERE r.PrincipalId = p.PrincipalId AND r.OrganizationId = p.OrganizationId
          AND r.RoleName IN ('Owner', 'IdentityAccessManager')
        UNION ALL
        SELECT 1 FROM dbo.IntegrationRoleAssignments r
        WHERE r.PrincipalId = p.PrincipalId AND r.OrganizationId = p.OrganizationId
          AND r.RoleName IN ('Owner', 'IdentityAccessManager')
        UNION ALL
        -- or through a group. RecursiveGroupMembers walks upward: RootGroupId is
        -- the group joined, GroupId a group that contains it.
        SELECT 1
        FROM dbo.GroupMembers gm
        INNER JOIN dbo.RecursiveGroupMembers rgm
            ON rgm.RootGroupId = gm.GroupId AND rgm.RootOrganizationId = gm.OrganizationId
        INNER JOIN (
            SELECT PrincipalId, OrganizationId, RoleName FROM dbo.OrganizationRoleAssignments
            UNION ALL SELECT PrincipalId, OrganizationId, RoleName FROM dbo.StackRoleAssignments
            UNION ALL SELECT PrincipalId, OrganizationId, RoleName FROM dbo.NamespaceRoleAssignments
            UNION ALL SELECT PrincipalId, OrganizationId, RoleName FROM dbo.ModuleRoleAssignments
            UNION ALL SELECT PrincipalId, OrganizationId, RoleName FROM dbo.RunnerRoleAssignments
            UNION ALL SELECT PrincipalId, OrganizationId, RoleName FROM dbo.AgentRoleAssignments
            UNION ALL SELECT PrincipalId, OrganizationId, RoleName FROM dbo.StateStoreRoleAssignments
            UNION ALL SELECT PrincipalId, OrganizationId, RoleName FROM dbo.IntegrationRoleAssignments
        ) r ON r.PrincipalId = rgm.GroupId AND r.OrganizationId = rgm.OrganizationId
        WHERE gm.PrincipalId = p.PrincipalId
          AND gm.OrganizationId = p.OrganizationId
          AND r.RoleName IN ('Owner', 'IdentityAccessManager'));

    -- give it to those who qualify and do not hold it
    INSERT INTO dbo.DerivedOrganizationRoleAssignments
        (PrincipalId, OrganizationId, RoleName, PrincipalDiscriminator)
    SELECT q.PrincipalId, q.OrganizationId, 'IdentityAccessMetadataReader', q.PrincipalDiscriminator
    FROM @Qualifying q
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.DerivedOrganizationRoleAssignments e
        WHERE e.PrincipalId = q.PrincipalId AND e.OrganizationId = q.OrganizationId
          AND e.RoleName = 'IdentityAccessMetadataReader');

    -- take it from those who hold it and no longer qualify
    -- only this role: another derivation's rows are not ours to clear
    DELETE e
    FROM dbo.DerivedOrganizationRoleAssignments e
    INNER JOIN @Principals p
        ON p.PrincipalId = e.PrincipalId AND p.OrganizationId = e.OrganizationId
    WHERE e.RoleName = 'IdentityAccessMetadataReader'
      AND NOT EXISTS (
        SELECT 1 FROM @Qualifying q
        WHERE q.PrincipalId = e.PrincipalId AND q.OrganizationId = e.OrganizationId);
END
GO

-- Step 2: a trigger per scope table. A row for a user or service principal
-- affects that principal; a row for a group affects everyone reaching it.
GO
CREATE OR ALTER TRIGGER dbo.trg_OrganizationRoleAssignments_IamMetadataReader
ON dbo.OrganizationRoleAssignments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Affected dbo.PrincipalScopeList;

    ;WITH touched AS (
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM inserted
        UNION
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM deleted
    )
    INSERT INTO @Affected (PrincipalId, OrganizationId, PrincipalDiscriminator)
    SELECT DISTINCT PrincipalId, OrganizationId, PrincipalDiscriminator
    FROM touched
    WHERE PrincipalDiscriminator <> 'Group'
    UNION
    -- members of a group that was assigned, reached through the closure
    SELECT DISTINCT gm.PrincipalId, gm.OrganizationId, gm.GroupMemberDiscriminator
    FROM touched t
    INNER JOIN dbo.RecursiveGroupMembers rgm
        ON rgm.GroupId = t.PrincipalId AND rgm.OrganizationId = t.OrganizationId
    INNER JOIN dbo.GroupMembers gm
        ON gm.GroupId = rgm.RootGroupId AND gm.OrganizationId = rgm.RootOrganizationId
    WHERE t.PrincipalDiscriminator = 'Group'
      AND gm.GroupMemberDiscriminator <> 'Group';

    EXEC dbo.usp_SyncIdentityAccessMetadataReaders @Principals = @Affected;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_StackRoleAssignments_IamMetadataReader
ON dbo.StackRoleAssignments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Affected dbo.PrincipalScopeList;

    ;WITH touched AS (
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM inserted
        UNION
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM deleted
    )
    INSERT INTO @Affected (PrincipalId, OrganizationId, PrincipalDiscriminator)
    SELECT DISTINCT PrincipalId, OrganizationId, PrincipalDiscriminator
    FROM touched
    WHERE PrincipalDiscriminator <> 'Group'
    UNION
    -- members of a group that was assigned, reached through the closure
    SELECT DISTINCT gm.PrincipalId, gm.OrganizationId, gm.GroupMemberDiscriminator
    FROM touched t
    INNER JOIN dbo.RecursiveGroupMembers rgm
        ON rgm.GroupId = t.PrincipalId AND rgm.OrganizationId = t.OrganizationId
    INNER JOIN dbo.GroupMembers gm
        ON gm.GroupId = rgm.RootGroupId AND gm.OrganizationId = rgm.RootOrganizationId
    WHERE t.PrincipalDiscriminator = 'Group'
      AND gm.GroupMemberDiscriminator <> 'Group';

    EXEC dbo.usp_SyncIdentityAccessMetadataReaders @Principals = @Affected;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_NamespaceRoleAssignments_IamMetadataReader
ON dbo.NamespaceRoleAssignments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Affected dbo.PrincipalScopeList;

    ;WITH touched AS (
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM inserted
        UNION
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM deleted
    )
    INSERT INTO @Affected (PrincipalId, OrganizationId, PrincipalDiscriminator)
    SELECT DISTINCT PrincipalId, OrganizationId, PrincipalDiscriminator
    FROM touched
    WHERE PrincipalDiscriminator <> 'Group'
    UNION
    -- members of a group that was assigned, reached through the closure
    SELECT DISTINCT gm.PrincipalId, gm.OrganizationId, gm.GroupMemberDiscriminator
    FROM touched t
    INNER JOIN dbo.RecursiveGroupMembers rgm
        ON rgm.GroupId = t.PrincipalId AND rgm.OrganizationId = t.OrganizationId
    INNER JOIN dbo.GroupMembers gm
        ON gm.GroupId = rgm.RootGroupId AND gm.OrganizationId = rgm.RootOrganizationId
    WHERE t.PrincipalDiscriminator = 'Group'
      AND gm.GroupMemberDiscriminator <> 'Group';

    EXEC dbo.usp_SyncIdentityAccessMetadataReaders @Principals = @Affected;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_ModuleRoleAssignments_IamMetadataReader
ON dbo.ModuleRoleAssignments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Affected dbo.PrincipalScopeList;

    ;WITH touched AS (
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM inserted
        UNION
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM deleted
    )
    INSERT INTO @Affected (PrincipalId, OrganizationId, PrincipalDiscriminator)
    SELECT DISTINCT PrincipalId, OrganizationId, PrincipalDiscriminator
    FROM touched
    WHERE PrincipalDiscriminator <> 'Group'
    UNION
    -- members of a group that was assigned, reached through the closure
    SELECT DISTINCT gm.PrincipalId, gm.OrganizationId, gm.GroupMemberDiscriminator
    FROM touched t
    INNER JOIN dbo.RecursiveGroupMembers rgm
        ON rgm.GroupId = t.PrincipalId AND rgm.OrganizationId = t.OrganizationId
    INNER JOIN dbo.GroupMembers gm
        ON gm.GroupId = rgm.RootGroupId AND gm.OrganizationId = rgm.RootOrganizationId
    WHERE t.PrincipalDiscriminator = 'Group'
      AND gm.GroupMemberDiscriminator <> 'Group';

    EXEC dbo.usp_SyncIdentityAccessMetadataReaders @Principals = @Affected;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_RunnerRoleAssignments_IamMetadataReader
ON dbo.RunnerRoleAssignments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Affected dbo.PrincipalScopeList;

    ;WITH touched AS (
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM inserted
        UNION
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM deleted
    )
    INSERT INTO @Affected (PrincipalId, OrganizationId, PrincipalDiscriminator)
    SELECT DISTINCT PrincipalId, OrganizationId, PrincipalDiscriminator
    FROM touched
    WHERE PrincipalDiscriminator <> 'Group'
    UNION
    -- members of a group that was assigned, reached through the closure
    SELECT DISTINCT gm.PrincipalId, gm.OrganizationId, gm.GroupMemberDiscriminator
    FROM touched t
    INNER JOIN dbo.RecursiveGroupMembers rgm
        ON rgm.GroupId = t.PrincipalId AND rgm.OrganizationId = t.OrganizationId
    INNER JOIN dbo.GroupMembers gm
        ON gm.GroupId = rgm.RootGroupId AND gm.OrganizationId = rgm.RootOrganizationId
    WHERE t.PrincipalDiscriminator = 'Group'
      AND gm.GroupMemberDiscriminator <> 'Group';

    EXEC dbo.usp_SyncIdentityAccessMetadataReaders @Principals = @Affected;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_AgentRoleAssignments_IamMetadataReader
ON dbo.AgentRoleAssignments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Affected dbo.PrincipalScopeList;

    ;WITH touched AS (
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM inserted
        UNION
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM deleted
    )
    INSERT INTO @Affected (PrincipalId, OrganizationId, PrincipalDiscriminator)
    SELECT DISTINCT PrincipalId, OrganizationId, PrincipalDiscriminator
    FROM touched
    WHERE PrincipalDiscriminator <> 'Group'
    UNION
    -- members of a group that was assigned, reached through the closure
    SELECT DISTINCT gm.PrincipalId, gm.OrganizationId, gm.GroupMemberDiscriminator
    FROM touched t
    INNER JOIN dbo.RecursiveGroupMembers rgm
        ON rgm.GroupId = t.PrincipalId AND rgm.OrganizationId = t.OrganizationId
    INNER JOIN dbo.GroupMembers gm
        ON gm.GroupId = rgm.RootGroupId AND gm.OrganizationId = rgm.RootOrganizationId
    WHERE t.PrincipalDiscriminator = 'Group'
      AND gm.GroupMemberDiscriminator <> 'Group';

    EXEC dbo.usp_SyncIdentityAccessMetadataReaders @Principals = @Affected;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_StateStoreRoleAssignments_IamMetadataReader
ON dbo.StateStoreRoleAssignments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Affected dbo.PrincipalScopeList;

    ;WITH touched AS (
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM inserted
        UNION
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM deleted
    )
    INSERT INTO @Affected (PrincipalId, OrganizationId, PrincipalDiscriminator)
    SELECT DISTINCT PrincipalId, OrganizationId, PrincipalDiscriminator
    FROM touched
    WHERE PrincipalDiscriminator <> 'Group'
    UNION
    -- members of a group that was assigned, reached through the closure
    SELECT DISTINCT gm.PrincipalId, gm.OrganizationId, gm.GroupMemberDiscriminator
    FROM touched t
    INNER JOIN dbo.RecursiveGroupMembers rgm
        ON rgm.GroupId = t.PrincipalId AND rgm.OrganizationId = t.OrganizationId
    INNER JOIN dbo.GroupMembers gm
        ON gm.GroupId = rgm.RootGroupId AND gm.OrganizationId = rgm.RootOrganizationId
    WHERE t.PrincipalDiscriminator = 'Group'
      AND gm.GroupMemberDiscriminator <> 'Group';

    EXEC dbo.usp_SyncIdentityAccessMetadataReaders @Principals = @Affected;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_IntegrationRoleAssignments_IamMetadataReader
ON dbo.IntegrationRoleAssignments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Affected dbo.PrincipalScopeList;

    ;WITH touched AS (
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM inserted
        UNION
        SELECT PrincipalId, OrganizationId, PrincipalDiscriminator FROM deleted
    )
    INSERT INTO @Affected (PrincipalId, OrganizationId, PrincipalDiscriminator)
    SELECT DISTINCT PrincipalId, OrganizationId, PrincipalDiscriminator
    FROM touched
    WHERE PrincipalDiscriminator <> 'Group'
    UNION
    -- members of a group that was assigned, reached through the closure
    SELECT DISTINCT gm.PrincipalId, gm.OrganizationId, gm.GroupMemberDiscriminator
    FROM touched t
    INNER JOIN dbo.RecursiveGroupMembers rgm
        ON rgm.GroupId = t.PrincipalId AND rgm.OrganizationId = t.OrganizationId
    INNER JOIN dbo.GroupMembers gm
        ON gm.GroupId = rgm.RootGroupId AND gm.OrganizationId = rgm.RootOrganizationId
    WHERE t.PrincipalDiscriminator = 'Group'
      AND gm.GroupMemberDiscriminator <> 'Group';

    EXEC dbo.usp_SyncIdentityAccessMetadataReaders @Principals = @Affected;
END
GO

-- Step 3: joining or leaving a group changes what a principal reaches.
CREATE OR ALTER TRIGGER dbo.trg_GroupMembers_IamMetadataReader
ON dbo.GroupMembers
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Affected dbo.PrincipalScopeList;

    ;WITH touched AS (
        SELECT PrincipalId, OrganizationId, GroupMemberDiscriminator, GroupId, MemberGroupId FROM inserted
        UNION
        SELECT PrincipalId, OrganizationId, GroupMemberDiscriminator, GroupId, MemberGroupId FROM deleted
    )
    INSERT INTO @Affected (PrincipalId, OrganizationId, PrincipalDiscriminator)
    SELECT DISTINCT PrincipalId, OrganizationId, GroupMemberDiscriminator
    FROM touched
    WHERE GroupMemberDiscriminator <> 'Group'
    UNION
    -- a group joining or leaving another group moves everyone inside it
    SELECT DISTINCT gm.PrincipalId, gm.OrganizationId, gm.GroupMemberDiscriminator
    FROM touched t
    INNER JOIN dbo.RecursiveGroupMembers rgm
        ON rgm.GroupId = t.MemberGroupId AND rgm.OrganizationId = t.OrganizationId
    INNER JOIN dbo.GroupMembers gm
        ON gm.GroupId = rgm.RootGroupId AND gm.OrganizationId = rgm.RootOrganizationId
    WHERE t.GroupMemberDiscriminator = 'Group'
      AND gm.GroupMemberDiscriminator <> 'Group';

    EXEC dbo.usp_SyncIdentityAccessMetadataReaders @Principals = @Affected;
END
GO

-- The closure this reads is rebuilt by its own trigger on the same table, and
-- trigger order is undefined, so make this one run last.
EXEC sp_settriggerorder @triggername = 'dbo.trg_GroupMembers_IamMetadataReader',
                        @order = 'Last', @stmttype = 'INSERT';
EXEC sp_settriggerorder @triggername = 'dbo.trg_GroupMembers_IamMetadataReader',
                        @order = 'Last', @stmttype = 'UPDATE';
EXEC sp_settriggerorder @triggername = 'dbo.trg_GroupMembers_IamMetadataReader',
                        @order = 'Last', @stmttype = 'DELETE';
GO

-- Step 4: nothing fires for principals who already qualify, so derive once.
DECLARE @Existing dbo.PrincipalScopeList;

-- One principal id can appear as more than one kind, so keep the first.
INSERT INTO @Existing (PrincipalId, OrganizationId, PrincipalDiscriminator)
SELECT PrincipalId, OrganizationId, PrincipalDiscriminator
FROM (
    SELECT PrincipalId, OrganizationId, PrincipalDiscriminator,
           ROW_NUMBER() OVER (PARTITION BY PrincipalId, OrganizationId ORDER BY PrincipalDiscriminator) AS rn
    FROM (
        SELECT UserId AS PrincipalId, OrganizationId, 'User' AS PrincipalDiscriminator FROM dbo.OrganizationUsers
        UNION ALL
        SELECT Id, OrganizationId, 'ServicePrincipal' FROM dbo.ServicePrincipals
        UNION ALL
        SELECT Id, OrganizationId, 'Group' FROM dbo.Groups
    ) all_principals
) deduped
WHERE rn = 1;

EXEC dbo.usp_SyncIdentityAccessMetadataReaders @Principals = @Existing;
GO
