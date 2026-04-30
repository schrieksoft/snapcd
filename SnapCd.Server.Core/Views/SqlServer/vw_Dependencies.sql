CREATE OR ALTER VIEW vw_Dependencies AS
WITH CurrentJobs AS (
    -- Get the most recent ModuleJob for each Module (may be running)
    SELECT
        mj.ModuleId,
        mj.ActualStateHeadline,
        ROW_NUMBER() OVER (PARTITION BY mj.ModuleId ORDER BY mj.TimestampStart DESC) as rn
    FROM ModuleJobs mj
    WHERE mj.IsCurrent = 1
),
LatestModuleJobs AS (
    -- Get the most recent COMPLETED ModuleJob for each Module (has ActualStateHeadline set)
    -- This gives us the "previous" state even when a job is currently running
    SELECT
        mj.ModuleId,
        COALESCE(mj.ActualStateHeadline, REPLACE(mj.JobType, 'JobSaga', '') + mj.Status) as ActualStateHeadline,
        ROW_NUMBER() OVER (PARTITION BY mj.ModuleId ORDER BY mj.TimestampEnd DESC) as rn
    FROM ModuleJobs mj
    WHERE mj.TimestampEnd IS NOT NULL
),

DependencyEdges AS (
    -- Dependencies from DependsOnModule (module -> dependency)
    SELECT DISTINCT
        DefinedModules.Id as DefinedModuleId,
        DefinedModules.OrganizationId as DefinedOrganizationId,
        DefinedModules.Name as DefinedModuleName,
        DefinedModules.NamespaceId as DefinedNamespaceId,
        DefinedNamespaces.Name as DefinedNamespaceName,
        DefinedNamespaces.StackId as DefinedStackId,
        DefinedStacks.Name as DefinedStackName,
        CONCAT(DefinedStacks.Name, '/', DefinedNamespaces.Name, '/', DefinedModules.Name) as DefinedDisplayName,
        DefinedLatestJobs.ActualStateHeadline as DefinedLatestActualState,
        DefinedModuleSagas.DesiredStateHeadline as DefinedDesiredState,
        DefinedModuleSagas.QueuedDesiredStateHeadline as DefinedQueuedDesiredState,
        CAST(CASE WHEN DefinedCurrentJobs.ModuleId IS NOT NULL THEN 1 ELSE 0 END AS BIT) as DefinedIsRunning,
        CAST(CASE WHEN DefinedModuleSagas.QueuedDesiredStateHeadline IS NOT NULL THEN 1 ELSE 0 END AS BIT) as DefinedIsQueued,
        CASE WHEN DefinedCurrentJobs.ModuleId IS NOT NULL THEN DefinedModuleSagas.DesiredStateHeadline ELSE NULL END as DefinedRunningDesiredState,
        DependsOnModules.DependsOnModuleId as ReferencedModuleId,
        ReferencedModules.OrganizationId as ReferencedOrganizationId,
        ReferencedModules.Name as ReferencedModuleName,
        ReferencedModules.NamespaceId as ReferencedNamespaceId,
        ReferencedNamespaces.Name as ReferencedNamespaceName,
        ReferencedNamespaces.StackId as ReferencedStackId,
        ReferencedStacks.Name as ReferencedStackName,
        CONCAT(ReferencedStacks.Name, '/', ReferencedNamespaces.Name, '/', ReferencedModules.Name) as ReferencedDisplayName,
        ReferencedLatestJobs.ActualStateHeadline as ReferencedLatestActualState,
        ReferencedModuleSagas.DesiredStateHeadline as ReferencedDesiredState,
        ReferencedModuleSagas.QueuedDesiredStateHeadline as ReferencedQueuedDesiredState,
        CAST(CASE WHEN ReferencedCurrentJobs.ModuleId IS NOT NULL THEN 1 ELSE 0 END AS BIT) as ReferencedIsRunning,
        CAST(CASE WHEN ReferencedModuleSagas.QueuedDesiredStateHeadline IS NOT NULL THEN 1 ELSE 0 END AS BIT) as ReferencedIsQueued,
        CASE WHEN ReferencedCurrentJobs.ModuleId IS NOT NULL THEN ReferencedModuleSagas.DesiredStateHeadline ELSE NULL END as ReferencedRunningDesiredState
    FROM DependsOnModules

    -- Defined
    INNER JOIN Modules DefinedModules ON DependsOnModules.ModuleId = DefinedModules.Id
    INNER JOIN Namespaces DefinedNamespaces ON DefinedModules.NamespaceId = DefinedNamespaces.Id
    INNER JOIN Stacks DefinedStacks ON DefinedNamespaces.StackId = DefinedStacks.Id
    LEFT JOIN CurrentJobs DefinedCurrentJobs ON DefinedCurrentJobs.ModuleId = DefinedModules.Id AND DefinedCurrentJobs.rn = 1
    LEFT JOIN LatestModuleJobs DefinedLatestJobs ON DefinedLatestJobs.ModuleId = DefinedModules.Id AND DefinedLatestJobs.rn = 1
    LEFT JOIN ModuleSagas DefinedModuleSagas ON DefinedModuleSagas.CorrelationId = DefinedModules.Id

    -- Referenced
    INNER JOIN Modules ReferencedModules ON DependsOnModules.DependsOnModuleId = ReferencedModules.Id
    INNER JOIN Namespaces ReferencedNamespaces ON ReferencedModules.NamespaceId = ReferencedNamespaces.Id
    INNER JOIN Stacks ReferencedStacks ON ReferencedNamespaces.StackId = ReferencedStacks.Id
    LEFT JOIN CurrentJobs ReferencedCurrentJobs ON ReferencedCurrentJobs.ModuleId = ReferencedModules.Id AND ReferencedCurrentJobs.rn = 1
    LEFT JOIN LatestModuleJobs ReferencedLatestJobs ON ReferencedLatestJobs.ModuleId = ReferencedModules.Id AND ReferencedLatestJobs.rn = 1
    LEFT JOIN ModuleSagas ReferencedModuleSagas ON ReferencedModuleSagas.CorrelationId = ReferencedModules.Id

    UNION

    -- Dependencies from ModuleInputFromOutput and ModuleInputFromOutputSet (consuming module -> producing module)
    SELECT DISTINCT
        -- Defined
        ModuleInputs.ModuleId as DefinedModuleId,
        DefinedModules.OrganizationId as DefinedOrganizationId,
        DefinedModules.Name as DefinedModuleName,
        DefinedModules.NamespaceId as DefinedNamespaceId,
        DefinedNamespaces.Name as DefinedNamespaceName,
        DefinedNamespaces.StackId as DefinedStackId,
        DefinedStacks.Name as DefinedStackName,
        CONCAT(DefinedStacks.Name, '/', DefinedNamespaces.Name, '/', DefinedModules.Name) as DefinedDisplayName,
        DefinedLatestJobs.ActualStateHeadline as DefinedLatestActualState,
        DefinedModuleSagas.DesiredStateHeadline as DefinedDesiredState,
        DefinedModuleSagas.QueuedDesiredStateHeadline as DefinedQueuedDesiredState,
        CAST(CASE WHEN DefinedCurrentJobs.ModuleId IS NOT NULL THEN 1 ELSE 0 END AS BIT) as DefinedIsRunning,
        CAST(CASE WHEN DefinedModuleSagas.QueuedDesiredStateHeadline IS NOT NULL THEN 1 ELSE 0 END AS BIT) as DefinedIsQueued,
        CASE WHEN DefinedCurrentJobs.ModuleId IS NOT NULL THEN DefinedModuleSagas.DesiredStateHeadline ELSE NULL END as DefinedRunningDesiredState,

        -- Referenced
        ModuleInputs.OutputModuleId as ReferencedModuleId,
        ReferencedModule.OrganizationId as ReferencedOrganizationId,
        ReferencedModule.Name as ReferencedModuleName,
        ReferencedModule.NamespaceId as ReferencedNamespaceId,
        ReferencedNamespace.Name as ReferencedNamespaceName,
        ReferencedNamespace.StackId as ReferencedStackId,
        ReferencedStack.Name as ReferencedStackName,
        CONCAT(ReferencedStack.Name, '/', ReferencedNamespace.Name, '/', ReferencedModule.Name) as ReferencedDisplayName,
        ReferencedLatestJobs.ActualStateHeadline as ReferencedLatestActualState,
        ReferencedModuleSagas.DesiredStateHeadline as ReferencedDesiredState,
        ReferencedModuleSagas.QueuedDesiredStateHeadline as ReferencedQueuedDesiredState,
        CAST(CASE WHEN ReferencedCurrentJobs.ModuleId IS NOT NULL THEN 1 ELSE 0 END AS BIT) as ReferencedIsRunning,
        CAST(CASE WHEN ReferencedModuleSagas.QueuedDesiredStateHeadline IS NOT NULL THEN 1 ELSE 0 END AS BIT) as ReferencedIsQueued,
        CASE WHEN ReferencedCurrentJobs.ModuleId IS NOT NULL THEN ReferencedModuleSagas.DesiredStateHeadline ELSE NULL END as ReferencedRunningDesiredState

    FROM ModuleInputs

    -- Defined
    INNER JOIN Modules DefinedModules ON ModuleInputs.ModuleId = DefinedModules.Id
    INNER JOIN Namespaces DefinedNamespaces ON DefinedModules.NamespaceId = DefinedNamespaces.Id
    INNER JOIN Stacks DefinedStacks ON DefinedNamespaces.StackId = DefinedStacks.Id
    LEFT JOIN CurrentJobs DefinedCurrentJobs ON DefinedCurrentJobs.ModuleId = DefinedModules.Id AND DefinedCurrentJobs.rn = 1
    LEFT JOIN LatestModuleJobs DefinedLatestJobs ON DefinedLatestJobs.ModuleId = DefinedModules.Id AND DefinedLatestJobs.rn = 1
    LEFT JOIN ModuleSagas DefinedModuleSagas ON DefinedModuleSagas.CorrelationId = DefinedModules.Id

    -- Referenced
    INNER JOIN Modules ReferencedModule ON ModuleInputs.OutputModuleId = ReferencedModule.Id
    INNER JOIN Namespaces ReferencedNamespace ON ReferencedModule.NamespaceId = ReferencedNamespace.Id
    INNER JOIN Stacks ReferencedStack ON ReferencedNamespace.StackId = ReferencedStack.Id
    LEFT JOIN CurrentJobs ReferencedCurrentJobs ON ReferencedCurrentJobs.ModuleId = ReferencedModule.Id AND ReferencedCurrentJobs.rn = 1
    LEFT JOIN LatestModuleJobs ReferencedLatestJobs ON ReferencedLatestJobs.ModuleId = ReferencedModule.Id AND ReferencedLatestJobs.rn = 1
    LEFT JOIN ModuleSagas ReferencedModuleSagas ON ReferencedModuleSagas.CorrelationId = ReferencedModule.Id

    WHERE ModuleInputs.Discriminator IN ('ModuleEnvVarFromOutput', 'ModuleParamFromOutput', 'ModuleParamFromOutputSet')
)
SELECT DISTINCT * FROM DependencyEdges;
