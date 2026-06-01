// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Services.DependencyGraph;

public class ApplyModuleGraphServiceFactory(IDbContextFactory<SnapCdDbContext> dbContextFactory)
{
    public ApplyModuleGraphService Create()
    {
        return new ApplyModuleGraphService(dbContextFactory.CreateDbContext());
    }
}

public class ApplyModuleGraphService : ModuleGraphServiceBase, IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public ApplyModuleGraphService(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public SnapCdDbContext GetDbContext()
    {
        return _dbContext;
    }

    /// <summary>
    /// Gets all dependencies that recursively depend on the given module for Apply operations
    /// Returns distinct dependency edges (deduplicates entries that appear at multiple depths)
    /// </summary>
    public async Task<List<Dependency>> ListRecursiveApplyDependenciesForReferencedModule(Guid moduleId)
    {
        // Use the recursive view to get all dependencies at once
        var recursiveDependencies = await _dbContext.RecursiveApplyDependencies
            .Where(rd => rd.RootModuleId == moduleId)
            .ToListAsync();

        if (!recursiveDependencies.Any())
            return new List<Dependency>();

        // Group by DefinedModuleId and ReferencedModuleId to get distinct edges
        // Take the first occurrence of each unique edge (lowest depth)
        var distinctEdges = recursiveDependencies
            .GroupBy(rd => new { rd.DefinedModuleId, rd.ReferencedModuleId })
            .Select(g => g.OrderBy(rd => rd.Depth).First()) // Take the one with the lowest depth
            .ToList();

        // Convert to Dependency objects
        var dependencies = distinctEdges.Select(Dependency.FromRecursive).ToList();

        return dependencies;
    }

    /// <summary>
    /// Gets all dependencies for modules that depend on modules within the given namespace for Apply operations
    /// Also includes dependencies where modules within the namespace depend on modules in other namespaces
    /// Returns distinct dependency edges for namespace-level apply dependency analysis
    /// </summary>
    public async Task<List<Dependency>> ListRecursiveApplyDependenciesForNamespace(Guid namespaceId)
    {
        // Get all dependencies where the namespace is involved (either as defined or referenced namespace)
        var recursiveDependencies = await _dbContext.RecursiveApplyDependencies
            .Where(rd => rd.ReferencedNamespaceId == namespaceId || rd.DefinedNamespaceId == namespaceId)
            .ToListAsync();

        if (!recursiveDependencies.Any())
            return new List<Dependency>();

        // Group by DefinedModuleId and ReferencedModuleId to get distinct edges
        // Take the first occurrence of each unique edge (lowest depth)
        var distinctEdges = recursiveDependencies
            .GroupBy(rd => new { rd.DefinedModuleId, rd.ReferencedModuleId })
            .Select(g => g.OrderBy(rd => rd.Depth).First()) // Take the one with the lowest depth
            .ToList();

        // Convert to Dependency objects
        var dependencies = distinctEdges.Select(Dependency.FromRecursive).ToList();

        return dependencies;
    }

    /// <summary>
    /// Builds an execution graph for applying modules with dependencies
    /// Similar to BuildDestroyGraphAsync but for Apply operations
    /// </summary>
    public async Task<ApplyModuleGraphDto> BuildApplyGraphAsync(Guid moduleId)
    {
        var dependencies = await ListRecursiveApplyDependenciesForReferencedModule(moduleId);

        // Get all unique modules involved
        var moduleIds = dependencies
            .SelectMany(d => new[] { d.DefinedModuleId, d.ReferencedModuleId })
            .Distinct()
            .ToList();

        if (!moduleIds.Contains(moduleId))
            moduleIds.Add(moduleId);

        var nodeStates = new List<ApplyModuleNodeDto>();

        foreach (var id in moduleIds)
        {
            var moduleInfo = dependencies
                .Where(d => d.DefinedModuleId == id || d.ReferencedModuleId == id)
                .FirstOrDefault();

            var isDefinedModule = moduleInfo?.DefinedModuleId == id;

            var nodeState = new ApplyModuleNodeDto
            {
                ModuleId = id,
                DisplayName = isDefinedModule
                    ? moduleInfo?.DefinedDisplayName ?? "Unknown"
                    : moduleInfo?.ReferencedDisplayName ?? "Unknown",
                ActualState = isDefinedModule
                    ? moduleInfo?.DefinedLatestActualState
                    : moduleInfo?.ReferencedLatestActualState,
                DependentModules = new List<string>(),
                DependencyModules = new List<string>()
            };

            // Get modules that depend on this one (defined modules where this is referenced)
            var dependentModules = dependencies
                .Where(d => d.ReferencedModuleId == id)
                .Select(d => d.DefinedDisplayName)
                .Distinct()
                .ToList();
            nodeState.DependentModules.AddRange(dependentModules);

            // Get modules this one depends on (referenced modules where this is defined)
            var dependencyModules = dependencies
                .Where(d => d.DefinedModuleId == id)
                .Select(d => d.ReferencedDisplayName)
                .Distinct()
                .ToList();
            nodeState.DependencyModules.AddRange(dependencyModules);

            nodeStates.Add(nodeState);
        }

        // Calculate stages for apply (topological sort)
        CalculateApplyStages(nodeStates, dependencies);

        return new ApplyModuleGraphDto
        {
            RootModuleId = moduleId,
            NodeStates = nodeStates,
            TotalModuleCount = nodeStates.Count,
            TotalStages = nodeStates.Any() ? nodeStates.Max(n => n.Stage) : 0
        };
    }

    private void CalculateApplyStages(List<ApplyModuleNodeDto> nodeStates, List<Dependency> dependencies)
    {
        var stages = new Dictionary<Guid, int>();
        var computed = new HashSet<Guid>();

        foreach (var nodeState in nodeStates)
            if (!computed.Contains(nodeState.ModuleId))
                CalculateApplyStageRecursive(nodeState.ModuleId, dependencies, stages, computed, new HashSet<Guid>());

        // Assign calculated stages (1-indexed for UI display)
        foreach (var nodeState in nodeStates) nodeState.Stage = stages.GetValueOrDefault(nodeState.ModuleId, 0) + 1;
    }

    private int CalculateApplyStageRecursive(Guid moduleId, List<Dependency> dependencies, Dictionary<Guid, int> stages, HashSet<Guid> computed, HashSet<Guid> currentPath)
    {
        // If already computed, return the stage
        if (stages.ContainsKey(moduleId))
            return stages[moduleId];

        // Check for cycles
        if (currentPath.Contains(moduleId))
        {
            stages[moduleId] = 0;
            computed.Add(moduleId);
            return 0;
        }

        currentPath.Add(moduleId);

        // For Apply: find modules that depend on this module (where this is ReferencedModule)
        // These modules must be applied AFTER this module
        var dependentEdges = dependencies.Where(d => d.ReferencedModuleId == moduleId).ToList();

        if (!dependentEdges.Any())
        {
            // No other modules depend on this - this is a leaf node (can be applied first, stage 0)
            stages[moduleId] = 0;
            computed.Add(moduleId);
            currentPath.Remove(moduleId);
            return 0;
        }

        // Calculate stage as max dependent stage + 1
        var maxDependentStage = -1;
        foreach (var edge in dependentEdges)
        {
            var dependentStage = CalculateApplyStageRecursive(edge.DefinedModuleId, dependencies, stages, computed, currentPath);
            maxDependentStage = Math.Max(maxDependentStage, dependentStage);
        }

        stages[moduleId] = Math.Max(0, maxDependentStage + 1);
        computed.Add(moduleId);
        currentPath.Remove(moduleId);
        return stages[moduleId];
    }

    /// <summary>
    /// Builds an apply dependency graph for all modules within a namespace
    /// </summary>
    public async Task<NamespaceDependencyGraphDto> BuildNamespaceApplyGraphAsync(Guid namespaceId, string namespaceName)
    {
        var dependencies = await ListRecursiveApplyDependenciesForNamespace(namespaceId);

        // Get all unique modules involved
        var moduleIds = dependencies
            .SelectMany(d => new[] { d.DefinedModuleId, d.ReferencedModuleId })
            .Distinct()
            .ToList();

        var nodeStates = new List<DependencyGraphNodeStateDto>();

        // Create a lookup for module information from the dependencies
        var moduleInfoLookup = BuildModuleInfoLookup(dependencies);

        foreach (var moduleId in moduleIds)
        {
            if (!moduleInfoLookup.TryGetValue(moduleId, out var moduleInfo))
                continue;

            var nodeState = CreateNodeState(moduleId, moduleInfo, DesiredStateHeadline.Applied);

            // Get incoming edges (dependencies) - what this module depends on (DefinedModuleId = this module)
            var incomingEdges = dependencies.Where(e => e.DefinedModuleId == moduleId).ToList();
            nodeState.IncomingEdges = incomingEdges
                .GroupBy(e => e.ReferencedModuleId)
                .Select(g => g.First())
                .Select(e => new DependencyGraphEdgeDto
                {
                    DisplayName = e.ReferencedDisplayName,
                    ModuleId = e.ReferencedModuleId,
                    NamespaceId = e.ReferencedNamespaceId
                })
                .ToList();

            // Get outgoing edges (dependents) - what depends on this module (ReferencedModuleId = this module)
            var outgoingEdges = dependencies.Where(e => e.ReferencedModuleId == moduleId).ToList();
            nodeState.OutgoingEdges = outgoingEdges
                .GroupBy(e => e.DefinedModuleId)
                .Select(g => g.First())
                .Select(e => new DependencyGraphEdgeDto
                {
                    DisplayName = e.DefinedDisplayName,
                    ModuleId = e.DefinedModuleId,
                    NamespaceId = e.DefinedNamespaceId
                })
                .ToList();

            nodeStates.Add(nodeState);
        }

        // Calculate stages using proper Apply logic
        CalculateApplyStagesForNodes(nodeStates, dependencies);

        return new NamespaceDependencyGraphDto
        {
            NamespaceId = namespaceId,
            NamespaceName = namespaceName,
            Direction = "Apply",
            TargetState = DesiredStateHeadline.Applied,
            NodeStates = nodeStates
        };
    }

    private void CalculateApplyStagesForNodes(List<DependencyGraphNodeStateDto> nodeStates, List<Dependency> dependencies)
    {
        var stages = new Dictionary<Guid, int>();
        var computed = new HashSet<Guid>();

        foreach (var nodeState in nodeStates)
            if (!computed.Contains(nodeState.ModuleId))
                CalculateApplyStageRecursiveForNodes(nodeState.ModuleId, dependencies, stages, computed, new HashSet<Guid>());

        // Assign calculated stages (1-indexed for UI display)
        foreach (var nodeState in nodeStates)
        {
            nodeState.Stage = stages.GetValueOrDefault(nodeState.ModuleId, 0) + 1;
            nodeState.StageOrdinal = GetOrdinalSuffix(nodeState.Stage);
        }
    }

    private int CalculateApplyStageRecursiveForNodes(Guid moduleId, List<Dependency> dependencies, Dictionary<Guid, int> stages, HashSet<Guid> computed, HashSet<Guid> currentPath)
    {
        // If already computed, return the stage
        if (stages.ContainsKey(moduleId))
            return stages[moduleId];

        // Check for cycles
        if (currentPath.Contains(moduleId))
        {
            stages[moduleId] = 0;
            computed.Add(moduleId);
            return 0;
        }

        currentPath.Add(moduleId);

        // For Apply: find what this module depends on (where this module is DefinedModule)
        // These dependencies must be applied BEFORE this module
        var dependencyEdges = dependencies.Where(d => d.DefinedModuleId == moduleId).ToList();

        if (!dependencyEdges.Any())
        {
            // No dependencies, can be applied first (stage 0)
            stages[moduleId] = 0;
            computed.Add(moduleId);
            currentPath.Remove(moduleId);
            return 0;
        }

        // Calculate stage as max dependency stage + 1
        var maxDependencyStage = -1;
        foreach (var edge in dependencyEdges)
        {
            var dependencyStage = CalculateApplyStageRecursiveForNodes(edge.ReferencedModuleId, dependencies, stages, computed, currentPath);
            maxDependencyStage = Math.Max(maxDependencyStage, dependencyStage);
        }

        stages[moduleId] = Math.Max(0, maxDependencyStage + 1);
        computed.Add(moduleId);
        currentPath.Remove(moduleId);
        return stages[moduleId];
    }

    /// <summary>
    /// Builds an apply dependency graph for a single module, showing all its dependencies
    /// </summary>
    public async Task<ModuleDependencyGraphDto> BuildModuleApplyGraphAsync(Guid moduleId, string moduleName)
    {
        var dependencies = await ListRecursiveApplyDependenciesForReferencedModule(moduleId);

        // Get all unique modules involved
        var moduleIds = dependencies
            .SelectMany(d => new[] { d.DefinedModuleId, d.ReferencedModuleId })
            .Distinct()
            .ToList();

        if (!moduleIds.Contains(moduleId))
            moduleIds.Add(moduleId);

        var nodeStates = new List<DependencyGraphNodeStateDto>();

        // Create a lookup for module information from the dependencies
        var moduleInfoLookup = BuildModuleInfoLookup(dependencies);

        // Add root module if it's not in the lookup (case where it has no dependencies)
        if (!moduleInfoLookup.ContainsKey(moduleId))
        {
            var rootModule = await _dbContext.Modules
                .Include(m => m.Namespace)
                .ThenInclude(n => n.Stack)
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (rootModule != null)
            {
                var rootModuleSaga = await _dbContext.ModuleSagas
                    .FirstOrDefaultAsync(ms => ms.CorrelationId == moduleId);

                var rootRunningJob = await _dbContext.ModuleJobs
                    .Where(mj => mj.ModuleId == moduleId && mj.IsCurrent == true)
                    .OrderByDescending(mj => mj.TimestampStart)
                    .FirstOrDefaultAsync();

                var latestCompletedJob = await _dbContext.ModuleJobs
                    .Where(mj => mj.ModuleId == moduleId && mj.TimestampEnd != null && mj.ActualStateHeadline != null)
                    .OrderByDescending(mj => mj.TimestampEnd)
                    .FirstOrDefaultAsync();

                moduleInfoLookup[moduleId] = new ModuleStateInfo
                {
                    ModuleId = moduleId,
                    Name = rootModule.Name,
                    NamespaceName = rootModule.Namespace.Name,
                    NamespaceId = rootModule.NamespaceId,
                    StackName = rootModule.Namespace.Stack.Name,
                    StackId = rootModule.Namespace.StackId,
                    DisplayName = $"{rootModule.Namespace.Stack.Name}/{rootModule.Namespace.Name}/{rootModule.Name}",
                    LatestActualState = latestCompletedJob?.ActualStateHeadline,
                    DesiredState = rootModuleSaga?.DesiredStateHeadline,
                    QueuedDesiredState = rootModuleSaga?.QueuedDesiredStateHeadline,
                    IsRunning = rootRunningJob?.IsCurrent == true,
                    IsQueued = rootModuleSaga?.QueuedDesiredStateHeadline != null,
                    RunningDesiredState = rootRunningJob?.IsCurrent == true ? rootModuleSaga?.DesiredStateHeadline : null
                };
            }
        }

        // Build node states
        foreach (var id in moduleIds)
        {
            if (!moduleInfoLookup.TryGetValue(id, out var moduleInfo))
                continue;

            var nodeState = CreateNodeState(id, moduleInfo, DesiredStateHeadline.Applied);

            // Add incoming edges (modules that depend on this one)
            var incomingEdges = dependencies
                .Where(d => d.ReferencedModuleId == id)
                .Select(d => new DependencyGraphEdgeDto
                {
                    ModuleId = d.DefinedModuleId,
                    DisplayName = d.DefinedDisplayName,
                    NamespaceId = d.DefinedNamespaceId
                })
                .Distinct()
                .ToList();
            nodeState.IncomingEdges.AddRange(incomingEdges);

            // Add outgoing edges (modules this one depends on)
            var outgoingEdges = dependencies
                .Where(d => d.DefinedModuleId == id)
                .Select(d => new DependencyGraphEdgeDto
                {
                    ModuleId = d.ReferencedModuleId,
                    DisplayName = d.ReferencedDisplayName,
                    NamespaceId = d.ReferencedNamespaceId
                })
                .Distinct()
                .ToList();
            nodeState.OutgoingEdges.AddRange(outgoingEdges);

            nodeStates.Add(nodeState);
        }

        // Calculate stages
        CalculateApplyStagesForNodes(nodeStates, dependencies);

        return new ModuleDependencyGraphDto
        {
            ModuleId = moduleId,
            ModuleName = moduleName,
            Direction = "Apply",
            TargetState = DesiredStateHeadline.Applied,
            NodeStates = nodeStates
        };
    }

    /// <summary>
    /// Builds an apply dependency graph for all modules within a stack
    /// </summary>
    public async Task<StackDependencyGraphDto> BuildStackApplyGraphAsync(Guid stackId, string stackName)
    {
        // Get direct dependencies for the stack using DependencyGraphService
        var directDependencyService = new DependencyGraphService(_dbContext);
        var dependencies = await directDependencyService.ListForDefinedStack(stackId);

        // Get all unique modules involved
        var moduleIds = dependencies
            .SelectMany(d => new[] { d.DefinedModuleId, d.ReferencedModuleId })
            .Distinct()
            .ToList();

        var nodeStates = new List<DependencyGraphNodeStateDto>();

        // Create a lookup for module information from the dependencies
        var moduleInfoLookup = BuildModuleInfoLookup(dependencies);

        foreach (var moduleId in moduleIds)
        {
            if (!moduleInfoLookup.TryGetValue(moduleId, out var moduleInfo))
                continue;

            var nodeState = CreateNodeState(moduleId, moduleInfo, DesiredStateHeadline.Applied);

            // Get incoming edges (dependencies) - what this module depends on (DefinedModuleId = this module)
            var incomingEdges = dependencies.Where(e => e.DefinedModuleId == moduleId).ToList();
            nodeState.IncomingEdges = incomingEdges
                .GroupBy(e => e.ReferencedModuleId)
                .Select(g => g.First())
                .Select(e => new DependencyGraphEdgeDto
                {
                    DisplayName = e.ReferencedDisplayName,
                    ModuleId = e.ReferencedModuleId,
                    NamespaceId = e.ReferencedNamespaceId
                })
                .ToList();

            // Get outgoing edges (dependents) - what depends on this module (ReferencedModuleId = this module)
            var outgoingEdges = dependencies.Where(e => e.ReferencedModuleId == moduleId).ToList();
            nodeState.OutgoingEdges = outgoingEdges
                .GroupBy(e => e.DefinedModuleId)
                .Select(g => g.First())
                .Select(e => new DependencyGraphEdgeDto
                {
                    DisplayName = e.DefinedDisplayName,
                    ModuleId = e.DefinedModuleId,
                    NamespaceId = e.DefinedNamespaceId
                })
                .ToList();

            nodeStates.Add(nodeState);
        }

        // Calculate stages using proper Apply logic
        CalculateStackApplyStages(nodeStates, dependencies);

        return new StackDependencyGraphDto
        {
            StackId = stackId,
            StackName = stackName,
            Direction = "Apply",
            TargetState = DesiredStateHeadline.Applied,
            NodeStates = nodeStates
        };
    }

    private void CalculateStackApplyStages(List<DependencyGraphNodeStateDto> nodeStates, List<Dependency> dependencies)
    {
        var stages = new Dictionary<Guid, int>();
        var computed = new HashSet<Guid>();

        foreach (var nodeState in nodeStates)
            if (!computed.Contains(nodeState.ModuleId))
                CalculateStackApplyStageRecursive(nodeState.ModuleId, dependencies, stages, computed, new HashSet<Guid>());

        // Assign calculated stages (1-indexed for UI display)
        foreach (var nodeState in nodeStates)
        {
            nodeState.Stage = stages.GetValueOrDefault(nodeState.ModuleId, 0) + 1;
            nodeState.StageOrdinal = GetOrdinalSuffix(nodeState.Stage);
        }
    }

    private int CalculateStackApplyStageRecursive(Guid moduleId, List<Dependency> dependencies, Dictionary<Guid, int> stages, HashSet<Guid> computed, HashSet<Guid> currentPath)
    {
        // If already computed, return the stage
        if (stages.ContainsKey(moduleId))
            return stages[moduleId];

        // Check for cycles
        if (currentPath.Contains(moduleId))
        {
            stages[moduleId] = 0;
            computed.Add(moduleId);
            return 0;
        }

        currentPath.Add(moduleId);

        // For Apply: find what this module depends on (where this module is DefinedModule)
        // These dependencies must be applied BEFORE this module
        var dependencyEdges = dependencies.Where(d => d.DefinedModuleId == moduleId).ToList();

        if (!dependencyEdges.Any())
        {
            // No dependencies, can be applied first (stage 0)
            stages[moduleId] = 0;
            computed.Add(moduleId);
            currentPath.Remove(moduleId);
            return 0;
        }

        // Calculate stage as max dependency stage + 1
        var maxDependencyStage = -1;
        foreach (var edge in dependencyEdges)
        {
            var dependencyStage = CalculateStackApplyStageRecursive(edge.ReferencedModuleId, dependencies, stages, computed, currentPath);
            maxDependencyStage = Math.Max(maxDependencyStage, dependencyStage);
        }

        stages[moduleId] = Math.Max(0, maxDependencyStage + 1);
        computed.Add(moduleId);
        currentPath.Remove(moduleId);
        return stages[moduleId];
    }


    public void Dispose()
    {
        _dbContext.Dispose();
    }
}