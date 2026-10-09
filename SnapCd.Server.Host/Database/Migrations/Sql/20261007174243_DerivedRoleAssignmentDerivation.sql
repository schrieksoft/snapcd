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
        -- a principal is its id and its kind: the same guid can be both a user
        -- and a service principal
        PRIMARY KEY (PrincipalId, OrganizationId, PrincipalDiscriminator));
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
        PRIMARY KEY (PrincipalId, OrganizationId, PrincipalDiscriminator));

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
          AND e.PrincipalDiscriminator = q.PrincipalDiscriminator
          AND e.RoleName = 'IdentityAccessMetadataReader');

    -- take it from those who hold it and no longer qualify
    -- only this role: another derivation's rows are not ours to clear
    DELETE e
    FROM dbo.DerivedOrganizationRoleAssignments e
    INNER JOIN @Principals p
        ON p.PrincipalId = e.PrincipalId AND p.OrganizationId = e.OrganizationId
        AND p.PrincipalDiscriminator = e.PrincipalDiscriminator
    WHERE e.RoleName = 'IdentityAccessMetadataReader'
      AND NOT EXISTS (
        SELECT 1 FROM @Qualifying q
        WHERE q.PrincipalId = e.PrincipalId AND q.OrganizationId = e.OrganizationId
          AND q.PrincipalDiscriminator = e.PrincipalDiscriminator);
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

-- ==========================================================================
-- Derived{Runner,Agent,Integration}RoleAssignments: metadata read on something
-- supplied to a scope the principal can already read.
--
-- Supplying a Runner to a Stack is what makes it usable there, so the people
-- working in that Stack should be able to name it without being granted a role
-- on the Runner itself.
--
-- Which scope the principal is looking at does not come into it: a role on one
-- Module is enough to read the metadata of anything supplied to that Module,
-- wherever they are in the UI. Scope cannot be enforced at read time anyway.
-- ==========================================================================

SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.types WHERE name = 'DerivedEntityList' AND is_table_type = 1)
    CREATE TYPE dbo.DerivedEntityList AS TABLE (
        EntityId uniqueidentifier NOT NULL PRIMARY KEY);
GO

IF NOT EXISTS (SELECT 1 FROM sys.types WHERE name = 'DerivedPrincipalList' AND is_table_type = 1)
    CREATE TYPE dbo.DerivedPrincipalList AS TABLE (
        PrincipalId uniqueidentifier NOT NULL PRIMARY KEY);
GO

-- Runner: reconciles only the entities and principals a change touched. An empty
-- list means every one of that side, which the backfill uses and nothing else.
CREATE OR ALTER PROCEDURE dbo.usp_SyncDerivedRunnerMetadataReaders
    @Entities dbo.DerivedEntityList READONLY,
    @Principals dbo.DerivedPrincipalList READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AnyEntity bit = CASE WHEN EXISTS (SELECT 1 FROM @Entities) THEN 0 ELSE 1 END;
    DECLARE @AnyPrincipal bit = CASE WHEN EXISTS (SELECT 1 FROM @Principals) THEN 0 ELSE 1 END;

    IF @AnyEntity = 1 AND @AnyPrincipal = 1 AND NOT EXISTS (SELECT 1 FROM dbo.Runners) RETURN;

    DECLARE @Qualifying TABLE (
        EntityId uniqueidentifier NOT NULL,
        PrincipalId uniqueidentifier NOT NULL,
        OrganizationId uniqueidentifier NOT NULL,
        PrincipalDiscriminator nvarchar(50) NOT NULL,
        PRIMARY KEY (EntityId, PrincipalId, OrganizationId, PrincipalDiscriminator));

    INSERT INTO @Qualifying (EntityId, PrincipalId, OrganizationId, PrincipalDiscriminator)
    SELECT DISTINCT EntityId, PrincipalId, OrganizationId, PrincipalDiscriminator
    FROM (
        SELECT s.RunnerId AS EntityId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.RunnerStackSupplies s
        INNER JOIN dbo.StackRoleAssignments ra
            ON ra.StackId = s.StackId AND ra.OrganizationId = s.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.RunnerId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        SELECT s.RunnerId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.RunnerNamespaceSupplies s
        INNER JOIN dbo.NamespaceRoleAssignments ra
            ON ra.NamespaceId = s.NamespaceId AND ra.OrganizationId = s.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.RunnerId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        SELECT s.RunnerId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.RunnerNamespaceSupplies s
        INNER JOIN dbo.Namespaces n ON n.Id = s.NamespaceId AND n.OrganizationId = s.OrganizationId
        INNER JOIN dbo.StackRoleAssignments ra
            ON ra.StackId = n.StackId AND ra.OrganizationId = n.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.RunnerId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        SELECT s.RunnerId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.RunnerModuleSupplies s
        INNER JOIN dbo.ModuleRoleAssignments ra
            ON ra.ModuleId = s.ModuleId AND ra.OrganizationId = s.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.RunnerId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        SELECT s.RunnerId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.RunnerModuleSupplies s
        INNER JOIN dbo.Modules m ON m.Id = s.ModuleId AND m.OrganizationId = s.OrganizationId
        INNER JOIN dbo.NamespaceRoleAssignments ra
            ON ra.NamespaceId = m.NamespaceId AND ra.OrganizationId = m.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.RunnerId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        SELECT s.RunnerId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.RunnerModuleSupplies s
        INNER JOIN dbo.Modules m ON m.Id = s.ModuleId AND m.OrganizationId = s.OrganizationId
        INNER JOIN dbo.Namespaces n ON n.Id = m.NamespaceId AND n.OrganizationId = m.OrganizationId
        INNER JOIN dbo.StackRoleAssignments ra
            ON ra.StackId = n.StackId AND ra.OrganizationId = n.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.RunnerId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        -- supplied to every Module, so any Module role reaches it
        SELECT e.Id, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.Runners e
        INNER JOIN dbo.ModuleRoleAssignments ra ON ra.OrganizationId = e.OrganizationId
        WHERE e.IsSuppliedToAllModules = 1
          AND ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR e.Id IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        -- an organization role reaches every scope, so it reaches anything supplied
        SELECT e.Id, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.Runners e
        INNER JOIN dbo.OrganizationRoleAssignments ra ON ra.OrganizationId = e.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader','StackContributor','StackReader')
          AND (@AnyEntity = 1 OR e.Id IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))
          AND (e.IsSuppliedToAllModules = 1
               OR EXISTS (SELECT 1 FROM dbo.RunnerStackSupplies x WHERE x.RunnerId = e.Id)
               OR EXISTS (SELECT 1 FROM dbo.RunnerNamespaceSupplies x WHERE x.RunnerId = e.Id)
               OR EXISTS (SELECT 1 FROM dbo.RunnerModuleSupplies x WHERE x.RunnerId = e.Id))
    ) reachable;

    -- only rows in the affected set, so an unrelated principal is left alone
    DELETE d
    FROM dbo.DerivedRunnerRoleAssignments d
    WHERE d.RoleName = 'MetadataReader'
      AND (@AnyEntity = 1 OR d.RunnerId IN (SELECT EntityId FROM @Entities))
      AND (@AnyPrincipal = 1 OR d.PrincipalId IN (SELECT PrincipalId FROM @Principals))
      AND NOT EXISTS (
        SELECT 1 FROM @Qualifying q
        WHERE q.EntityId = d.RunnerId AND q.PrincipalId = d.PrincipalId
          AND q.OrganizationId = d.OrganizationId
          AND q.PrincipalDiscriminator = d.PrincipalDiscriminator);

    INSERT INTO dbo.DerivedRunnerRoleAssignments
        (RunnerId, PrincipalId, OrganizationId, RoleName, PrincipalDiscriminator)
    SELECT q.EntityId, q.PrincipalId, q.OrganizationId, 'MetadataReader', q.PrincipalDiscriminator
    FROM @Qualifying q
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.DerivedRunnerRoleAssignments d
        WHERE d.RunnerId = q.EntityId AND d.PrincipalId = q.PrincipalId
          AND d.OrganizationId = q.OrganizationId AND d.RoleName = 'MetadataReader'
          AND d.PrincipalDiscriminator = q.PrincipalDiscriminator);
END
GO

-- Agent: reconciles only the entities and principals a change touched. An empty
-- list means every one of that side, which the backfill uses and nothing else.
CREATE OR ALTER PROCEDURE dbo.usp_SyncDerivedAgentMetadataReaders
    @Entities dbo.DerivedEntityList READONLY,
    @Principals dbo.DerivedPrincipalList READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AnyEntity bit = CASE WHEN EXISTS (SELECT 1 FROM @Entities) THEN 0 ELSE 1 END;
    DECLARE @AnyPrincipal bit = CASE WHEN EXISTS (SELECT 1 FROM @Principals) THEN 0 ELSE 1 END;

    IF @AnyEntity = 1 AND @AnyPrincipal = 1 AND NOT EXISTS (SELECT 1 FROM dbo.Agents) RETURN;

    DECLARE @Qualifying TABLE (
        EntityId uniqueidentifier NOT NULL,
        PrincipalId uniqueidentifier NOT NULL,
        OrganizationId uniqueidentifier NOT NULL,
        PrincipalDiscriminator nvarchar(50) NOT NULL,
        PRIMARY KEY (EntityId, PrincipalId, OrganizationId, PrincipalDiscriminator));

    INSERT INTO @Qualifying (EntityId, PrincipalId, OrganizationId, PrincipalDiscriminator)
    SELECT DISTINCT EntityId, PrincipalId, OrganizationId, PrincipalDiscriminator
    FROM (
        SELECT s.AgentId AS EntityId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.AgentStackSupplies s
        INNER JOIN dbo.StackRoleAssignments ra
            ON ra.StackId = s.StackId AND ra.OrganizationId = s.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.AgentId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        SELECT s.AgentId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.AgentNamespaceSupplies s
        INNER JOIN dbo.NamespaceRoleAssignments ra
            ON ra.NamespaceId = s.NamespaceId AND ra.OrganizationId = s.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.AgentId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        SELECT s.AgentId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.AgentNamespaceSupplies s
        INNER JOIN dbo.Namespaces n ON n.Id = s.NamespaceId AND n.OrganizationId = s.OrganizationId
        INNER JOIN dbo.StackRoleAssignments ra
            ON ra.StackId = n.StackId AND ra.OrganizationId = n.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.AgentId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        SELECT s.AgentId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.AgentModuleSupplies s
        INNER JOIN dbo.ModuleRoleAssignments ra
            ON ra.ModuleId = s.ModuleId AND ra.OrganizationId = s.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.AgentId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        SELECT s.AgentId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.AgentModuleSupplies s
        INNER JOIN dbo.Modules m ON m.Id = s.ModuleId AND m.OrganizationId = s.OrganizationId
        INNER JOIN dbo.NamespaceRoleAssignments ra
            ON ra.NamespaceId = m.NamespaceId AND ra.OrganizationId = m.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.AgentId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        SELECT s.AgentId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.AgentModuleSupplies s
        INNER JOIN dbo.Modules m ON m.Id = s.ModuleId AND m.OrganizationId = s.OrganizationId
        INNER JOIN dbo.Namespaces n ON n.Id = m.NamespaceId AND n.OrganizationId = m.OrganizationId
        INNER JOIN dbo.StackRoleAssignments ra
            ON ra.StackId = n.StackId AND ra.OrganizationId = n.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.AgentId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        -- supplied to every Module, so any Module role reaches it
        SELECT e.Id, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.Agents e
        INNER JOIN dbo.ModuleRoleAssignments ra ON ra.OrganizationId = e.OrganizationId
        WHERE e.IsSuppliedToAllModules = 1
          AND ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR e.Id IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        -- an organization role reaches every scope, so it reaches anything supplied
        SELECT e.Id, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.Agents e
        INNER JOIN dbo.OrganizationRoleAssignments ra ON ra.OrganizationId = e.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader','StackContributor','StackReader')
          AND (@AnyEntity = 1 OR e.Id IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))
          AND (e.IsSuppliedToAllModules = 1
               OR EXISTS (SELECT 1 FROM dbo.AgentStackSupplies x WHERE x.AgentId = e.Id)
               OR EXISTS (SELECT 1 FROM dbo.AgentNamespaceSupplies x WHERE x.AgentId = e.Id)
               OR EXISTS (SELECT 1 FROM dbo.AgentModuleSupplies x WHERE x.AgentId = e.Id))
    ) reachable;

    -- only rows in the affected set, so an unrelated principal is left alone
    DELETE d
    FROM dbo.DerivedAgentRoleAssignments d
    WHERE d.RoleName = 'MetadataReader'
      AND (@AnyEntity = 1 OR d.AgentId IN (SELECT EntityId FROM @Entities))
      AND (@AnyPrincipal = 1 OR d.PrincipalId IN (SELECT PrincipalId FROM @Principals))
      AND NOT EXISTS (
        SELECT 1 FROM @Qualifying q
        WHERE q.EntityId = d.AgentId AND q.PrincipalId = d.PrincipalId
          AND q.OrganizationId = d.OrganizationId
          AND q.PrincipalDiscriminator = d.PrincipalDiscriminator);

    INSERT INTO dbo.DerivedAgentRoleAssignments
        (AgentId, PrincipalId, OrganizationId, RoleName, PrincipalDiscriminator)
    SELECT q.EntityId, q.PrincipalId, q.OrganizationId, 'MetadataReader', q.PrincipalDiscriminator
    FROM @Qualifying q
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.DerivedAgentRoleAssignments d
        WHERE d.AgentId = q.EntityId AND d.PrincipalId = q.PrincipalId
          AND d.OrganizationId = q.OrganizationId AND d.RoleName = 'MetadataReader'
          AND d.PrincipalDiscriminator = q.PrincipalDiscriminator);
END
GO

-- Integration: reconciles only the entities and principals a change touched. An empty
-- list means every one of that side, which the backfill uses and nothing else.
CREATE OR ALTER PROCEDURE dbo.usp_SyncDerivedIntegrationMetadataReaders
    @Entities dbo.DerivedEntityList READONLY,
    @Principals dbo.DerivedPrincipalList READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AnyEntity bit = CASE WHEN EXISTS (SELECT 1 FROM @Entities) THEN 0 ELSE 1 END;
    DECLARE @AnyPrincipal bit = CASE WHEN EXISTS (SELECT 1 FROM @Principals) THEN 0 ELSE 1 END;

    IF @AnyEntity = 1 AND @AnyPrincipal = 1 AND NOT EXISTS (SELECT 1 FROM dbo.Integrations) RETURN;

    DECLARE @Qualifying TABLE (
        EntityId uniqueidentifier NOT NULL,
        PrincipalId uniqueidentifier NOT NULL,
        OrganizationId uniqueidentifier NOT NULL,
        PrincipalDiscriminator nvarchar(50) NOT NULL,
        PRIMARY KEY (EntityId, PrincipalId, OrganizationId, PrincipalDiscriminator));

    INSERT INTO @Qualifying (EntityId, PrincipalId, OrganizationId, PrincipalDiscriminator)
    SELECT DISTINCT EntityId, PrincipalId, OrganizationId, PrincipalDiscriminator
    FROM (
        SELECT s.IntegrationId AS EntityId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.IntegrationStackSupplies s
        INNER JOIN dbo.StackRoleAssignments ra
            ON ra.StackId = s.StackId AND ra.OrganizationId = s.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.IntegrationId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        SELECT s.IntegrationId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.IntegrationNamespaceSupplies s
        INNER JOIN dbo.NamespaceRoleAssignments ra
            ON ra.NamespaceId = s.NamespaceId AND ra.OrganizationId = s.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.IntegrationId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        SELECT s.IntegrationId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.IntegrationNamespaceSupplies s
        INNER JOIN dbo.Namespaces n ON n.Id = s.NamespaceId AND n.OrganizationId = s.OrganizationId
        INNER JOIN dbo.StackRoleAssignments ra
            ON ra.StackId = n.StackId AND ra.OrganizationId = n.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.IntegrationId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        SELECT s.IntegrationId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.IntegrationModuleSupplies s
        INNER JOIN dbo.ModuleRoleAssignments ra
            ON ra.ModuleId = s.ModuleId AND ra.OrganizationId = s.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.IntegrationId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        SELECT s.IntegrationId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.IntegrationModuleSupplies s
        INNER JOIN dbo.Modules m ON m.Id = s.ModuleId AND m.OrganizationId = s.OrganizationId
        INNER JOIN dbo.NamespaceRoleAssignments ra
            ON ra.NamespaceId = m.NamespaceId AND ra.OrganizationId = m.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.IntegrationId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        SELECT s.IntegrationId, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.IntegrationModuleSupplies s
        INNER JOIN dbo.Modules m ON m.Id = s.ModuleId AND m.OrganizationId = s.OrganizationId
        INNER JOIN dbo.Namespaces n ON n.Id = m.NamespaceId AND n.OrganizationId = m.OrganizationId
        INNER JOIN dbo.StackRoleAssignments ra
            ON ra.StackId = n.StackId AND ra.OrganizationId = n.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR s.IntegrationId IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        -- supplied to every Module, so any Module role reaches it
        SELECT e.Id, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.Integrations e
        INNER JOIN dbo.ModuleRoleAssignments ra ON ra.OrganizationId = e.OrganizationId
        WHERE e.IsSuppliedToAllModules = 1
          AND ra.RoleName IN ('Owner','Contributor','Reader')
          AND (@AnyEntity = 1 OR e.Id IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))

        UNION

        -- an organization role reaches every scope, so it reaches anything supplied
        SELECT e.Id, ra.PrincipalId, ra.OrganizationId, ra.PrincipalDiscriminator
        FROM dbo.Integrations e
        INNER JOIN dbo.OrganizationRoleAssignments ra ON ra.OrganizationId = e.OrganizationId
        WHERE ra.RoleName IN ('Owner','Contributor','Reader','StackContributor','StackReader')
          AND (@AnyEntity = 1 OR e.Id IN (SELECT EntityId FROM @Entities))
          AND (@AnyPrincipal = 1 OR ra.PrincipalId IN (SELECT PrincipalId FROM @Principals))
          AND (e.IsSuppliedToAllModules = 1
               OR EXISTS (SELECT 1 FROM dbo.IntegrationStackSupplies x WHERE x.IntegrationId = e.Id)
               OR EXISTS (SELECT 1 FROM dbo.IntegrationNamespaceSupplies x WHERE x.IntegrationId = e.Id)
               OR EXISTS (SELECT 1 FROM dbo.IntegrationModuleSupplies x WHERE x.IntegrationId = e.Id))
    ) reachable;

    -- only rows in the affected set, so an unrelated principal is left alone
    DELETE d
    FROM dbo.DerivedIntegrationRoleAssignments d
    WHERE d.RoleName = 'MetadataReader'
      AND (@AnyEntity = 1 OR d.IntegrationId IN (SELECT EntityId FROM @Entities))
      AND (@AnyPrincipal = 1 OR d.PrincipalId IN (SELECT PrincipalId FROM @Principals))
      AND NOT EXISTS (
        SELECT 1 FROM @Qualifying q
        WHERE q.EntityId = d.IntegrationId AND q.PrincipalId = d.PrincipalId
          AND q.OrganizationId = d.OrganizationId
          AND q.PrincipalDiscriminator = d.PrincipalDiscriminator);

    INSERT INTO dbo.DerivedIntegrationRoleAssignments
        (IntegrationId, PrincipalId, OrganizationId, RoleName, PrincipalDiscriminator)
    SELECT q.EntityId, q.PrincipalId, q.OrganizationId, 'MetadataReader', q.PrincipalDiscriminator
    FROM @Qualifying q
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.DerivedIntegrationRoleAssignments d
        WHERE d.IntegrationId = q.EntityId AND d.PrincipalId = q.PrincipalId
          AND d.OrganizationId = q.OrganizationId AND d.RoleName = 'MetadataReader'
          AND d.PrincipalDiscriminator = q.PrincipalDiscriminator);
END
GO

-- Triggers. Each hands over only what it touched: the entity whose supply moved,
-- or the principal whose roles changed. Nothing here rebuilds the whole table.
GO
CREATE OR ALTER TRIGGER dbo.trg_RunnerStackSupplies_SupplyMetadataReader
ON dbo.RunnerStackSupplies
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Entities (EntityId)
    SELECT RunnerId FROM inserted
    UNION
    SELECT RunnerId FROM deleted;

    EXEC dbo.usp_SyncDerivedRunnerMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_RunnerNamespaceSupplies_SupplyMetadataReader
ON dbo.RunnerNamespaceSupplies
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Entities (EntityId)
    SELECT RunnerId FROM inserted
    UNION
    SELECT RunnerId FROM deleted;

    EXEC dbo.usp_SyncDerivedRunnerMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_RunnerModuleSupplies_SupplyMetadataReader
ON dbo.RunnerModuleSupplies
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Entities (EntityId)
    SELECT RunnerId FROM inserted
    UNION
    SELECT RunnerId FROM deleted;

    EXEC dbo.usp_SyncDerivedRunnerMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_AgentStackSupplies_SupplyMetadataReader
ON dbo.AgentStackSupplies
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Entities (EntityId)
    SELECT AgentId FROM inserted
    UNION
    SELECT AgentId FROM deleted;

    EXEC dbo.usp_SyncDerivedAgentMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_AgentNamespaceSupplies_SupplyMetadataReader
ON dbo.AgentNamespaceSupplies
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Entities (EntityId)
    SELECT AgentId FROM inserted
    UNION
    SELECT AgentId FROM deleted;

    EXEC dbo.usp_SyncDerivedAgentMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_AgentModuleSupplies_SupplyMetadataReader
ON dbo.AgentModuleSupplies
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Entities (EntityId)
    SELECT AgentId FROM inserted
    UNION
    SELECT AgentId FROM deleted;

    EXEC dbo.usp_SyncDerivedAgentMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_IntegrationStackSupplies_SupplyMetadataReader
ON dbo.IntegrationStackSupplies
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Entities (EntityId)
    SELECT IntegrationId FROM inserted
    UNION
    SELECT IntegrationId FROM deleted;

    EXEC dbo.usp_SyncDerivedIntegrationMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_IntegrationNamespaceSupplies_SupplyMetadataReader
ON dbo.IntegrationNamespaceSupplies
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Entities (EntityId)
    SELECT IntegrationId FROM inserted
    UNION
    SELECT IntegrationId FROM deleted;

    EXEC dbo.usp_SyncDerivedIntegrationMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_IntegrationModuleSupplies_SupplyMetadataReader
ON dbo.IntegrationModuleSupplies
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Entities (EntityId)
    SELECT IntegrationId FROM inserted
    UNION
    SELECT IntegrationId FROM deleted;

    EXEC dbo.usp_SyncDerivedIntegrationMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_Runners_SupplyMetadataReader
ON dbo.Runners
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT UPDATE(IsSuppliedToAllModules) AND EXISTS (SELECT 1 FROM deleted) AND EXISTS (SELECT 1 FROM inserted)
        RETURN;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Entities (EntityId)
    SELECT Id FROM inserted
    UNION
    SELECT Id FROM deleted;

    EXEC dbo.usp_SyncDerivedRunnerMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_Agents_SupplyMetadataReader
ON dbo.Agents
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT UPDATE(IsSuppliedToAllModules) AND EXISTS (SELECT 1 FROM deleted) AND EXISTS (SELECT 1 FROM inserted)
        RETURN;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Entities (EntityId)
    SELECT Id FROM inserted
    UNION
    SELECT Id FROM deleted;

    EXEC dbo.usp_SyncDerivedAgentMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_Integrations_SupplyMetadataReader
ON dbo.Integrations
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT UPDATE(IsSuppliedToAllModules) AND EXISTS (SELECT 1 FROM deleted) AND EXISTS (SELECT 1 FROM inserted)
        RETURN;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Entities (EntityId)
    SELECT Id FROM inserted
    UNION
    SELECT Id FROM deleted;

    EXEC dbo.usp_SyncDerivedIntegrationMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_OrganizationRoleAssignments_SupplyMetadataReader
ON dbo.OrganizationRoleAssignments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Principals (PrincipalId)
    SELECT PrincipalId FROM inserted
    UNION
    SELECT PrincipalId FROM deleted;

    EXEC dbo.usp_SyncDerivedRunnerMetadataReaders @Entities = @Entities, @Principals = @Principals;
    EXEC dbo.usp_SyncDerivedAgentMetadataReaders @Entities = @Entities, @Principals = @Principals;
    EXEC dbo.usp_SyncDerivedIntegrationMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_StackRoleAssignments_SupplyMetadataReader
ON dbo.StackRoleAssignments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Principals (PrincipalId)
    SELECT PrincipalId FROM inserted
    UNION
    SELECT PrincipalId FROM deleted;

    EXEC dbo.usp_SyncDerivedRunnerMetadataReaders @Entities = @Entities, @Principals = @Principals;
    EXEC dbo.usp_SyncDerivedAgentMetadataReaders @Entities = @Entities, @Principals = @Principals;
    EXEC dbo.usp_SyncDerivedIntegrationMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_NamespaceRoleAssignments_SupplyMetadataReader
ON dbo.NamespaceRoleAssignments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Principals (PrincipalId)
    SELECT PrincipalId FROM inserted
    UNION
    SELECT PrincipalId FROM deleted;

    EXEC dbo.usp_SyncDerivedRunnerMetadataReaders @Entities = @Entities, @Principals = @Principals;
    EXEC dbo.usp_SyncDerivedAgentMetadataReaders @Entities = @Entities, @Principals = @Principals;
    EXEC dbo.usp_SyncDerivedIntegrationMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_ModuleRoleAssignments_SupplyMetadataReader
ON dbo.ModuleRoleAssignments
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Principals (PrincipalId)
    SELECT PrincipalId FROM inserted
    UNION
    SELECT PrincipalId FROM deleted;

    EXEC dbo.usp_SyncDerivedRunnerMetadataReaders @Entities = @Entities, @Principals = @Principals;
    EXEC dbo.usp_SyncDerivedAgentMetadataReaders @Entities = @Entities, @Principals = @Principals;
    EXEC dbo.usp_SyncDerivedIntegrationMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
CREATE OR ALTER TRIGGER dbo.trg_GroupMembers_SupplyMetadataReader
ON dbo.GroupMembers
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Entities dbo.DerivedEntityList;
    DECLARE @Principals dbo.DerivedPrincipalList;

    INSERT INTO @Principals (PrincipalId)
    SELECT PrincipalId FROM inserted
    UNION
    SELECT PrincipalId FROM deleted;

    EXEC dbo.usp_SyncDerivedRunnerMetadataReaders @Entities = @Entities, @Principals = @Principals;
    EXEC dbo.usp_SyncDerivedAgentMetadataReaders @Entities = @Entities, @Principals = @Principals;
    EXEC dbo.usp_SyncDerivedIntegrationMetadataReaders @Entities = @Entities, @Principals = @Principals;
END
GO
