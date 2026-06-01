-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

CREATE OR ALTER VIEW [dbo].[vw_RecursiveDestroyDependencies] AS
WITH RecursiveDependencies AS (
    -- Anchor: start from each dependency edge, treating the Referenced as the root
    SELECT
        -- Root module details (the module being depended on)
        d.ReferencedModuleId         AS RootModuleId,
        d.ReferencedOrganizationId   AS RootOrganizationId,
        d.ReferencedModuleName       AS RootModuleName,
        d.ReferencedNamespaceId      AS RootNamespaceId,
        d.ReferencedNamespaceName    AS RootNamespaceName,
        d.ReferencedStackId          AS RootStackId,
        d.ReferencedStackName        AS RootStackName,
        d.ReferencedDisplayName      AS RootDisplayName,
        d.ReferencedLatestActualState AS RootLatestActualState,
        d.ReferencedDesiredState     AS RootDesiredState,
        d.ReferencedQueuedDesiredState AS RootQueuedDesiredState,
        d.ReferencedIsRunning        AS RootIsRunning,
        d.ReferencedIsQueued         AS RootIsQueued,
        d.ReferencedRunningDesiredState AS RootRunningDesiredState,

        -- Current edge: Defined depends on Referenced
        d.DefinedModuleId,
        d.DefinedOrganizationId,
        d.DefinedModuleName,
        d.DefinedNamespaceId,
        d.DefinedNamespaceName,
        d.DefinedStackId,
        d.DefinedStackName,
        d.DefinedDisplayName,
        d.DefinedLatestActualState,
        d.DefinedDesiredState,
        d.DefinedQueuedDesiredState,
        d.DefinedIsRunning,
        d.DefinedIsQueued,
        d.DefinedRunningDesiredState,

        d.ReferencedModuleId,
        d.ReferencedOrganizationId,
        d.ReferencedModuleName,
        d.ReferencedNamespaceId,
        d.ReferencedNamespaceName,
        d.ReferencedStackId,
        d.ReferencedStackName,
        d.ReferencedDisplayName,
        d.ReferencedLatestActualState,
        d.ReferencedDesiredState,
        d.ReferencedQueuedDesiredState,
        d.ReferencedIsRunning,
        d.ReferencedIsQueued,
        d.ReferencedRunningDesiredState,

        1 AS Depth,
        CAST('|' + CAST(d.ReferencedModuleId AS VARCHAR(36)) + '|' + CAST(d.DefinedModuleId AS VARCHAR(36)) + '|' AS NVARCHAR(MAX)) AS VisitedPath
    FROM dbo.vw_Dependencies d

    UNION ALL

    -- Recursive step: follow 'who depends on me?'
    SELECT
        -- Root module details (always preserved)
        r.RootModuleId,
        r.RootOrganizationId,
        r.RootModuleName,
        r.RootNamespaceId,
        r.RootNamespaceName,
        r.RootStackId,
        r.RootStackName,
        r.RootDisplayName,
        r.RootLatestActualState,
        r.RootDesiredState,
        r.RootQueuedDesiredState,
        r.RootIsRunning,
        r.RootIsQueued,
        r.RootRunningDesiredState,

        -- Current Defined becomes the new dependent module
        d.DefinedModuleId,
        d.DefinedOrganizationId,
        d.DefinedModuleName,
        d.DefinedNamespaceId,
        d.DefinedNamespaceName,
        d.DefinedStackId,
        d.DefinedStackName,
        d.DefinedDisplayName,
        d.DefinedLatestActualState,
        d.DefinedDesiredState,
        d.DefinedQueuedDesiredState,
        d.DefinedIsRunning,
        d.DefinedIsQueued,
        d.DefinedRunningDesiredState,

        -- Current Referenced comes from the join
        d.ReferencedModuleId,
        d.ReferencedOrganizationId,
        d.ReferencedModuleName,
        d.ReferencedNamespaceId,
        d.ReferencedNamespaceName,
        d.ReferencedStackId,
        d.ReferencedStackName,
        d.ReferencedDisplayName,
        d.ReferencedLatestActualState,
        d.ReferencedDesiredState,
        d.ReferencedQueuedDesiredState,
        d.ReferencedIsRunning,
        d.ReferencedIsQueued,
        d.ReferencedRunningDesiredState,

        r.Depth + 1,
        CAST(r.VisitedPath + CAST(d.DefinedModuleId AS VARCHAR(36)) + '|' AS NVARCHAR(MAX))
    FROM RecursiveDependencies r
    INNER JOIN dbo.vw_Dependencies d
        ON r.DefinedModuleId = d.ReferencedModuleId
    WHERE CHARINDEX('|' + CAST(d.DefinedModuleId AS VARCHAR(36)) + '|', r.VisitedPath) = 0
)
SELECT *
FROM RecursiveDependencies;
