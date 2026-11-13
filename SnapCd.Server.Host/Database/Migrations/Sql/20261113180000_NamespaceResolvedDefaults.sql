-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

-- ==========================================================================
-- Each Namespace's defaults as its subtree sees them: the nearest Namespace
-- that sets a value, unless one further up pins it.
--
-- A view rather than a table, so there is nothing to invalidate. It reads
-- NamespaceAncestors, which the triggers keep current.
--
-- One pass per setting through ROW_NUMBER: a pinned value sorts ahead of an
-- unpinned one, and among pinned values the furthest wins, which is what
-- "a parent refuses an override below it" means.
-- ==========================================================================

SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER VIEW dbo.NamespaceResolvedDefaults
AS
WITH Candidates AS
(
    SELECT
        a.NamespaceId,
        a.OrganizationId,
        a.Depth,
        n.DefaultCleanInitEnabled,
        n.DefaultTriggerPathFilterEnabled,
        n.DefaultApplyApprovalThreshold,
        n.DefaultDestroyApprovalThreshold,
        n.DefaultStateMigrationApprovalThreshold,
        n.DefaultApprovalTimeoutMinutes,
        n.DefaultEngine,
        n.DefaultDriftCheckEnabled,
        n.DefaultDriftCheckIntervalMinutes,
        n.DefaultOutputSetDownstreamRestriction,
        n.RestrictOverrideOnCleanInitEnabled,
        n.RestrictOverrideOnTriggerPathFilterEnabled,
        n.RestrictOverrideOnApplyApprovalThreshold,
        n.RestrictOverrideOnDestroyApprovalThreshold,
        n.RestrictOverrideOnStateMigrationApprovalThreshold,
        n.RestrictOverrideOnApprovalTimeoutMinutes,
        n.RestrictOverrideOnEngine,
        n.RestrictOverrideOnDriftCheckEnabled,
        n.RestrictOverrideOnDriftCheckIntervalMinutes,
        n.RestrictOverrideOnOutputSetDownstreamRestriction
    FROM dbo.NamespaceAncestors a
    INNER JOIN dbo.Namespaces n
        ON n.Id = a.AncestorNamespaceId
        AND n.OrganizationId = a.OrganizationId
),
Ranked AS
(
    SELECT
        NamespaceId,
        OrganizationId,
        DefaultCleanInitEnabled,
        DefaultTriggerPathFilterEnabled,
        DefaultApplyApprovalThreshold,
        DefaultDestroyApprovalThreshold,
        DefaultStateMigrationApprovalThreshold,
        DefaultApprovalTimeoutMinutes,
        DefaultEngine,
        DefaultDriftCheckEnabled,
        DefaultDriftCheckIntervalMinutes,
        DefaultOutputSetDownstreamRestriction,

        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN DefaultCleanInitEnabled IS NULL THEN 2
                          WHEN RestrictOverrideOnCleanInitEnabled = 1 THEN 0 ELSE 1 END,
                     CASE WHEN RestrictOverrideOnCleanInitEnabled = 1 THEN -Depth ELSE Depth END) AS RnCleanInit,

        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN DefaultTriggerPathFilterEnabled IS NULL THEN 2
                          WHEN RestrictOverrideOnTriggerPathFilterEnabled = 1 THEN 0 ELSE 1 END,
                     CASE WHEN RestrictOverrideOnTriggerPathFilterEnabled = 1 THEN -Depth ELSE Depth END) AS RnTriggerPathFilter,

        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN DefaultApplyApprovalThreshold IS NULL THEN 2
                          WHEN RestrictOverrideOnApplyApprovalThreshold = 1 THEN 0 ELSE 1 END,
                     CASE WHEN RestrictOverrideOnApplyApprovalThreshold = 1 THEN -Depth ELSE Depth END) AS RnApply,

        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN DefaultDestroyApprovalThreshold IS NULL THEN 2
                          WHEN RestrictOverrideOnDestroyApprovalThreshold = 1 THEN 0 ELSE 1 END,
                     CASE WHEN RestrictOverrideOnDestroyApprovalThreshold = 1 THEN -Depth ELSE Depth END) AS RnDestroy,

        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN DefaultStateMigrationApprovalThreshold IS NULL THEN 2
                          WHEN RestrictOverrideOnStateMigrationApprovalThreshold = 1 THEN 0 ELSE 1 END,
                     CASE WHEN RestrictOverrideOnStateMigrationApprovalThreshold = 1 THEN -Depth ELSE Depth END) AS RnStateMigration,

        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN DefaultApprovalTimeoutMinutes IS NULL THEN 2
                          WHEN RestrictOverrideOnApprovalTimeoutMinutes = 1 THEN 0 ELSE 1 END,
                     CASE WHEN RestrictOverrideOnApprovalTimeoutMinutes = 1 THEN -Depth ELSE Depth END) AS RnTimeout,

        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN DefaultEngine IS NULL THEN 2
                          WHEN RestrictOverrideOnEngine = 1 THEN 0 ELSE 1 END,
                     CASE WHEN RestrictOverrideOnEngine = 1 THEN -Depth ELSE Depth END) AS RnEngine,

        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN DefaultDriftCheckEnabled IS NULL THEN 2
                          WHEN RestrictOverrideOnDriftCheckEnabled = 1 THEN 0 ELSE 1 END,
                     CASE WHEN RestrictOverrideOnDriftCheckEnabled = 1 THEN -Depth ELSE Depth END) AS RnDrift,

        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN DefaultDriftCheckIntervalMinutes IS NULL THEN 2
                          WHEN RestrictOverrideOnDriftCheckIntervalMinutes = 1 THEN 0 ELSE 1 END,
                     CASE WHEN RestrictOverrideOnDriftCheckIntervalMinutes = 1 THEN -Depth ELSE Depth END) AS RnDriftInterval,

        ROW_NUMBER() OVER (PARTITION BY NamespaceId, OrganizationId
            ORDER BY CASE WHEN DefaultOutputSetDownstreamRestriction IS NULL THEN 2
                          WHEN RestrictOverrideOnOutputSetDownstreamRestriction = 1 THEN 0 ELSE 1 END,
                     CASE WHEN RestrictOverrideOnOutputSetDownstreamRestriction = 1 THEN -Depth ELSE Depth END) AS RnRestriction
    FROM Candidates
)
SELECT
    NamespaceId,
    OrganizationId,
    CAST(MAX(CASE WHEN RnCleanInit = 1 THEN CAST(DefaultCleanInitEnabled AS INT) END) AS BIT)AS CleanInitEnabled,
    CAST(MAX(CASE WHEN RnTriggerPathFilter = 1 THEN CAST(DefaultTriggerPathFilterEnabled AS INT) END) AS BIT)AS TriggerPathFilterEnabled,
    MAX(CASE WHEN RnApply            = 1 THEN DefaultApplyApprovalThreshold END)          AS ApplyApprovalThreshold,
    MAX(CASE WHEN RnDestroy          = 1 THEN DefaultDestroyApprovalThreshold END)        AS DestroyApprovalThreshold,
    MAX(CASE WHEN RnStateMigration   = 1 THEN DefaultStateMigrationApprovalThreshold END) AS StateMigrationApprovalThreshold,
    MAX(CASE WHEN RnTimeout          = 1 THEN DefaultApprovalTimeoutMinutes END)          AS ApprovalTimeoutMinutes,
    MAX(CASE WHEN RnEngine           = 1 THEN DefaultEngine END)                          AS Engine,
    CAST(MAX(CASE WHEN RnDrift = 1 THEN CAST(DefaultDriftCheckEnabled AS INT) END) AS BIT)AS DriftCheckEnabled,
    MAX(CASE WHEN RnDriftInterval    = 1 THEN DefaultDriftCheckIntervalMinutes END)       AS DriftCheckIntervalMinutes,
    MAX(CASE WHEN RnRestriction      = 1 THEN DefaultOutputSetDownstreamRestriction END)  AS OutputSetDownstreamRestriction
FROM Ranked
GROUP BY NamespaceId, OrganizationId;
GO
