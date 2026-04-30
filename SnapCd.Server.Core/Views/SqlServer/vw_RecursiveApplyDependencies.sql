CREATE OR ALTER VIEW [dbo].[vw_RecursiveApplyDependencies] AS
WITH RecursiveDependencies AS (
    -- Anchor: start from each dependency edge, treating Defined as the root
    SELECT
        -- Root module details (the module that has dependencies)
        d.DefinedModuleId            AS RootModuleId,
        d.DefinedOrganizationId      AS RootOrganizationId,
        d.DefinedModuleName          AS RootModuleName,
        d.DefinedNamespaceId         AS RootNamespaceId,
        d.DefinedNamespaceName       AS RootNamespaceName,
        d.DefinedStackId             AS RootStackId,
        d.DefinedStackName           AS RootStackName,
        d.DefinedDisplayName         AS RootDisplayName,
        d.DefinedLatestActualState   AS RootLatestActualState,
        d.DefinedDesiredState        AS RootDesiredState,
        d.DefinedQueuedDesiredState  AS RootQueuedDesiredState,
        d.DefinedIsRunning           AS RootIsRunning,
        d.DefinedIsQueued            AS RootIsQueued,
        d.DefinedRunningDesiredState AS RootRunningDesiredState,

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

        1                            AS Depth,
        CAST('|' + CAST(d.DefinedModuleId AS VARCHAR(36)) + '|' + CAST(d.ReferencedModuleId AS VARCHAR(36)) +
             '|' AS NVARCHAR(MAX))   AS VisitedPath
    FROM dbo.vw_Dependencies d

    UNION ALL

    -- Recursive step: follow 'who do I depend on?'
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

        -- Current edge at this step
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

        r.Depth + 1,
        CAST(r.VisitedPath + CAST(d.ReferencedModuleId AS VARCHAR(36)) + '|' AS NVARCHAR(MAX))
    FROM RecursiveDependencies r
             INNER JOIN dbo.vw_Dependencies d
                        ON r.ReferencedModuleId = d.DefinedModuleId
    WHERE CHARINDEX('|' + CAST(d.ReferencedModuleId AS VARCHAR(36)) + '|', r.VisitedPath) = 0)
SELECT *
FROM RecursiveDependencies;
