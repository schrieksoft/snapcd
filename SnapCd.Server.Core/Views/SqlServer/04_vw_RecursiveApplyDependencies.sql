-- SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
-- Copyright (c) 2026 Karl Schriek / Schrieksoft.
-- No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
-- embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
-- system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
-- Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
-- for terms covering either use.

CREATE OR ALTER VIEW [dbo].[vw_RecursiveApplyDependencies] AS
-- Real recursive edges
SELECT
    re.RootModuleId,
    re.RootOrganizationId,
    re.RootModuleName,
    re.RootNamespaceId,
    re.RootNamespaceName,
    re.RootStackId,
    re.RootStackName,
    re.RootDisplayName,
    RootState.LatestActualStateHeadline AS RootLatestActualState,
    RootState.DesiredStateHeadline AS RootDesiredState,
    RootState.QueuedDesiredStateHeadline AS RootQueuedDesiredState,
    COALESCE(RootState.IsRunning, CAST(0 AS BIT)) AS RootIsRunning,
    CAST(CASE WHEN RootState.QueuedDesiredStateHeadline IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS RootIsQueued,
    CASE WHEN RootState.IsRunning = 1 THEN RootState.DesiredStateHeadline ELSE NULL END AS RootRunningDesiredState,

    re.DefinedModuleId,
    re.DefinedOrganizationId,
    re.DefinedModuleName,
    re.DefinedNamespaceId,
    re.DefinedNamespaceName,
    re.DefinedStackId,
    re.DefinedStackName,
    re.DefinedDisplayName,
    DefinedState.LatestActualStateHeadline AS DefinedLatestActualState,
    DefinedState.DesiredStateHeadline AS DefinedDesiredState,
    DefinedState.QueuedDesiredStateHeadline AS DefinedQueuedDesiredState,
    COALESCE(DefinedState.IsRunning, CAST(0 AS BIT)) AS DefinedIsRunning,
    CAST(CASE WHEN DefinedState.QueuedDesiredStateHeadline IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS DefinedIsQueued,
    CASE WHEN DefinedState.IsRunning = 1 THEN DefinedState.DesiredStateHeadline ELSE NULL END AS DefinedRunningDesiredState,

    CAST(re.ReferencedModuleId AS UNIQUEIDENTIFIER) AS ReferencedModuleId,
    CAST(re.ReferencedOrganizationId AS UNIQUEIDENTIFIER) AS ReferencedOrganizationId,
    CAST(re.ReferencedModuleName AS NVARCHAR(450)) AS ReferencedModuleName,
    CAST(re.ReferencedNamespaceId AS UNIQUEIDENTIFIER) AS ReferencedNamespaceId,
    CAST(re.ReferencedNamespaceName AS NVARCHAR(450)) AS ReferencedNamespaceName,
    CAST(re.ReferencedStackId AS UNIQUEIDENTIFIER) AS ReferencedStackId,
    CAST(re.ReferencedStackName AS NVARCHAR(450)) AS ReferencedStackName,
    CAST(re.ReferencedDisplayName AS NVARCHAR(MAX)) AS ReferencedDisplayName,
    ReferencedState.LatestActualStateHeadline AS ReferencedLatestActualState,
    ReferencedState.DesiredStateHeadline AS ReferencedDesiredState,
    ReferencedState.QueuedDesiredStateHeadline AS ReferencedQueuedDesiredState,
    ReferencedState.IsRunning AS ReferencedIsRunning,
    CAST(CASE WHEN ReferencedState.QueuedDesiredStateHeadline IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS ReferencedIsQueued,
    CASE WHEN ReferencedState.IsRunning = 1 THEN ReferencedState.DesiredStateHeadline ELSE NULL END AS ReferencedRunningDesiredState,

    re.Depth

FROM RecursiveDependencyEdges re

LEFT JOIN ModuleState RootState ON RootState.ModuleId = re.RootModuleId
LEFT JOIN ModuleState DefinedState ON DefinedState.ModuleId = re.DefinedModuleId
LEFT JOIN ModuleState ReferencedState ON ReferencedState.ModuleId = re.ReferencedModuleId

WHERE re.Direction = 1

UNION ALL

-- Standalone modules (no dependency edges)
SELECT
    m.Id AS RootModuleId,
    m.OrganizationId AS RootOrganizationId,
    m.Name AS RootModuleName,
    ns.Id AS RootNamespaceId,
    ns.Name AS RootNamespaceName,
    st.Id AS RootStackId,
    st.Name AS RootStackName,
    CONCAT(st.Name, '/', ns.Name, '/', m.Name) AS RootDisplayName,
    ms.LatestActualStateHeadline AS RootLatestActualState,
    ms.DesiredStateHeadline AS RootDesiredState,
    ms.QueuedDesiredStateHeadline AS RootQueuedDesiredState,
    COALESCE(ms.IsRunning, CAST(0 AS BIT)) AS RootIsRunning,
    CAST(CASE WHEN ms.QueuedDesiredStateHeadline IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS RootIsQueued,
    CASE WHEN ms.IsRunning = 1 THEN ms.DesiredStateHeadline ELSE NULL END AS RootRunningDesiredState,

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
    CAST(NULL AS NVARCHAR(MAX)) AS ReferencedRunningDesiredState,

    0 AS Depth

FROM Modules m
INNER JOIN Namespaces ns ON ns.Id = m.NamespaceId
INNER JOIN Stacks st ON st.Id = ns.StackId
LEFT JOIN ModuleState ms ON ms.ModuleId = m.Id
WHERE NOT EXISTS (
    SELECT 1 FROM DependencyEdges de
    WHERE de.DefinedModuleId = m.Id OR de.ReferencedModuleId = m.Id
);
