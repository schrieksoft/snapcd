-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

CREATE OR ALTER VIEW vw_Dependencies AS
-- Real dependency edges
SELECT DISTINCT
    e.DefinedModuleId,
    e.DefinedOrganizationId,
    e.DefinedModuleName,
    e.DefinedNamespaceId,
    e.DefinedNamespaceName,
    e.DefinedStackId,
    e.DefinedStackName,
    e.DefinedDisplayName,
    DefinedState.LatestActualStateHeadline AS DefinedLatestActualState,
    DefinedState.DesiredStateHeadline AS DefinedDesiredState,
    DefinedState.QueuedDesiredStateHeadline AS DefinedQueuedDesiredState,
    COALESCE(DefinedState.IsRunning, CAST(0 AS BIT)) AS DefinedIsRunning,
    CAST(CASE WHEN DefinedState.QueuedDesiredStateHeadline IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS DefinedIsQueued,
    CASE WHEN DefinedState.IsRunning = 1 THEN DefinedState.DesiredStateHeadline ELSE NULL END AS DefinedRunningDesiredState,

    CAST(e.ReferencedModuleId AS UNIQUEIDENTIFIER) AS ReferencedModuleId,
    CAST(e.ReferencedOrganizationId AS UNIQUEIDENTIFIER) AS ReferencedOrganizationId,
    CAST(e.ReferencedModuleName AS NVARCHAR(450)) AS ReferencedModuleName,
    CAST(e.ReferencedNamespaceId AS UNIQUEIDENTIFIER) AS ReferencedNamespaceId,
    CAST(e.ReferencedNamespaceName AS NVARCHAR(450)) AS ReferencedNamespaceName,
    CAST(e.ReferencedStackId AS UNIQUEIDENTIFIER) AS ReferencedStackId,
    CAST(e.ReferencedStackName AS NVARCHAR(450)) AS ReferencedStackName,
    CAST(e.ReferencedDisplayName AS NVARCHAR(MAX)) AS ReferencedDisplayName,
    ReferencedState.LatestActualStateHeadline AS ReferencedLatestActualState,
    ReferencedState.DesiredStateHeadline AS ReferencedDesiredState,
    ReferencedState.QueuedDesiredStateHeadline AS ReferencedQueuedDesiredState,
    ReferencedState.IsRunning AS ReferencedIsRunning,
    CAST(CASE WHEN ReferencedState.QueuedDesiredStateHeadline IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS ReferencedIsQueued,
    CASE WHEN ReferencedState.IsRunning = 1 THEN ReferencedState.DesiredStateHeadline ELSE NULL END AS ReferencedRunningDesiredState

FROM vw_DependencyEdges e

LEFT JOIN ModuleState DefinedState ON DefinedState.ModuleId = e.DefinedModuleId
LEFT JOIN ModuleState ReferencedState ON ReferencedState.ModuleId = e.ReferencedModuleId

UNION ALL

-- Standalone modules (no dependency edges)
SELECT
    m.Id AS DefinedModuleId,
    m.OrganizationId AS DefinedOrganizationId,
    m.Name AS DefinedModuleName,
    ns.Id AS DefinedNamespaceId,
    ns.Name AS DefinedNamespaceName,
    st.Id AS DefinedStackId,
    st.Name AS DefinedStackName,
    CONCAT(st.Name, '/', ns.Name, '/', m.Name) AS DefinedDisplayName,
    ms.LatestActualStateHeadline AS DefinedLatestActualState,
    ms.DesiredStateHeadline AS DefinedDesiredState,
    ms.QueuedDesiredStateHeadline AS DefinedQueuedDesiredState,
    COALESCE(ms.IsRunning, CAST(0 AS BIT)) AS DefinedIsRunning,
    CAST(CASE WHEN ms.QueuedDesiredStateHeadline IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS DefinedIsQueued,
    CASE WHEN ms.IsRunning = 1 THEN ms.DesiredStateHeadline ELSE NULL END AS DefinedRunningDesiredState,

    CAST(NULL AS UNIQUEIDENTIFIER) AS ReferencedModuleId,
    CAST(NULL AS UNIQUEIDENTIFIER) AS ReferencedOrganizationId,
    CAST(NULL AS NVARCHAR(450)) AS ReferencedModuleName,
    CAST(NULL AS UNIQUEIDENTIFIER) AS ReferencedNamespaceId,
    CAST(NULL AS NVARCHAR(450)) AS ReferencedNamespaceName,
    CAST(NULL AS UNIQUEIDENTIFIER) AS ReferencedStackId,
    CAST(NULL AS NVARCHAR(450)) AS ReferencedStackName,
    CAST(NULL AS NVARCHAR(MAX)) AS ReferencedDisplayName,
    CAST(NULL AS NVARCHAR(MAX)) AS ReferencedLatestActualState,
    CAST(NULL AS NVARCHAR(MAX)) AS ReferencedDesiredState,
    CAST(NULL AS NVARCHAR(MAX)) AS ReferencedQueuedDesiredState,
    CAST(NULL AS BIT) AS ReferencedIsRunning,
    CAST(NULL AS BIT) AS ReferencedIsQueued,
    CAST(NULL AS NVARCHAR(MAX)) AS ReferencedRunningDesiredState

FROM Modules m
INNER JOIN Namespaces ns ON ns.Id = m.NamespaceId
INNER JOIN Stacks st ON st.Id = ns.StackId
LEFT JOIN ModuleState ms ON ms.ModuleId = m.Id
WHERE NOT EXISTS (
    SELECT 1 FROM DependencyEdges de
    WHERE de.DefinedModuleId = m.Id OR de.ReferencedModuleId = m.Id
);
