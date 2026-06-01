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
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Services.DependencyGraph;

public class DestroyModuleGraphServiceFactory(IDbContextFactory<SnapCdDbContext> dbContextFactory)
{
    public DestroyModuleGraphService Create()
    {
        return new DestroyModuleGraphService(dbContextFactory.CreateDbContext());
    }
}

public class DestroyModuleGraphService : ModuleGraphServiceBase, IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public DestroyModuleGraphService(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public SnapCdDbContext GetDbContext()
    {
        return _dbContext;
    }

    /// <summary>
    /// Gets all dependencies that recursively depend on the given module
    /// Returns distinct dependency edges (deduplicates entries that appear at multiple depths)
    /// </summary>
    public async Task<List<Dependency>> ListRecursiveDestroyDependenciesForReferencedModule(Guid moduleId)
    {
        // Use the recursive view to get all dependencies at once
        var recursiveDependencies = await _dbContext.RecursiveDestroyDependencies
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
    /// Builds a dependency graph for module destruction, starting from the root module
    /// and finding all modules that transitively depend on it
    /// </summary>
    public async Task<DestroyModuleGraphDto> BuildDestroyGraphAsync(Guid rootModuleId)
    {
        // Get recursive destroy dependencies using the optimized view
        var allEdges = await ListRecursiveDestroyDependenciesForReferencedModule(rootModuleId);

        // Get the root module information
        var rootModule = await _dbContext.Modules
            .Include(m => m.Namespace)
            .ThenInclude(n => n.Stack)
            .FirstOrDefaultAsync(m => m.Id == rootModuleId);

        if (rootModule == null) throw new ArgumentException($"Module with ID {rootModuleId} not found");

        // Get all unique module IDs from the dependency edges, including the root module
        var moduleIds = allEdges.SelectMany(e => new[] { e.DefinedModuleId, e.ReferencedModuleId }).Distinct().ToList();
        moduleIds.Add(rootModuleId); // Ensure root module is included even if it has no dependencies
        moduleIds = moduleIds.Distinct().ToList();

        // Create node states using data from the recursive dependencies
        var nodeStates = new List<DestroyModuleNodeDto>();

        // Create a lookup for module information from the edges
        var moduleInfoLookup = BuildModuleInfoLookup(allEdges);

        // Add root module info if it's not already in the lookup (case where root has no dependencies)
        if (!moduleInfoLookup.ContainsKey(rootModuleId))
            moduleInfoLookup[rootModuleId] = new ModuleStateInfo
            {
                ModuleId = rootModuleId,
                Name = rootModule.Name,
                NamespaceName = rootModule.Namespace.Name,
                NamespaceId = rootModule.NamespaceId,
                StackName = rootModule.Namespace.Stack.Name,
                StackId = rootModule.Namespace.StackId,
                DisplayName = $"{rootModule.Namespace.Stack.Name}/{rootModule.Namespace.Name}/{rootModule.Name}"
            };

        foreach (var moduleId in moduleIds)
        {
            var moduleInfo = moduleInfoLookup[moduleId];
            var nodeState = new DestroyModuleNodeDto
            {
                ModuleId = moduleId,
                ModuleName = moduleInfo.Name,
                NamespaceId = moduleInfo.NamespaceId,
                NamespaceName = moduleInfo.NamespaceName,
                StackId = moduleInfo.StackId,
                StackName = moduleInfo.StackName,
                DisplayName = moduleInfo.DisplayName,
                ActualState = moduleInfo.LatestActualState ?? ActualStateHeadline.None,
                DesiredState = moduleInfo.DesiredState
            };

            // Find dependent modules (modules that depend on this one)
            var dependentEdges = allEdges.Where(e => e.ReferencedModuleId == moduleId);
            nodeState.DependentModules = dependentEdges
                .Select(e => e.DefinedDisplayName)
                .Distinct()
                .ToList();

            // Find dependency modules (modules this one depends on)
            var dependencyEdges = allEdges.Where(e => e.DefinedModuleId == moduleId);
            nodeState.DependencyModules = dependencyEdges
                .Select(e => e.ReferencedDisplayName)
                .Distinct()
                .ToList();

            nodeStates.Add(nodeState);
        }

        // Calculate stages for destruction (reverse of apply order)
        CalculateDestructionStages(nodeStates, allEdges.Where(e => moduleIds.Contains(e.DefinedModuleId) && moduleIds.Contains(e.ReferencedModuleId)).ToList());

        var result = new DestroyModuleGraphDto
        {
            RootModuleId = rootModuleId,
            RootModuleName = rootModule.Name,
            NodeStates = nodeStates.OrderBy(n => n.Stage).ThenBy(n => n.DisplayName).ToList()
        };

        return result;
    }


    private void CalculateDestructionStages(List<DestroyModuleNodeDto> nodeStates, List<Dependency> edges)
    {
        var stages = new Dictionary<Guid, int>();
        var computed = new HashSet<Guid>();

        // Calculate stages for each module
        foreach (var nodeState in nodeStates)
            if (!computed.Contains(nodeState.ModuleId))
                CalculateDestructionStageRecursive(nodeState.ModuleId, edges, stages, computed, new HashSet<Guid>());

        // Assign calculated stages (1-indexed for UI display)
        foreach (var nodeState in nodeStates) nodeState.Stage = stages.GetValueOrDefault(nodeState.ModuleId, 0) + 1;
    }

    private int CalculateDestructionStageRecursive(Guid moduleId, List<Dependency> edges, Dictionary<Guid, int> stages, HashSet<Guid> computed, HashSet<Guid> currentPath)
    {
        if (computed.Contains(moduleId)) return stages[moduleId];

        if (currentPath.Contains(moduleId))
        {
            // Circular dependency detected - assign stage 0
            stages[moduleId] = 0;
            computed.Add(moduleId);
            return 0;
        }

        currentPath.Add(moduleId);

        // For destruction, we need to destroy dependent modules first
        // So find modules that depend on this module (where this module is ReferencedModule)
        var dependentEdges = edges.Where(e => e.ReferencedModuleId == moduleId).ToList();

        if (!dependentEdges.Any())
        {
            // No dependent modules, can be destroyed first (stage 0)
            stages[moduleId] = 0;
        }
        else
        {
            // Calculate stage as max dependent stage + 1
            var maxDependentStage = -1;
            foreach (var edge in dependentEdges)
            {
                var dependentStage = CalculateDestructionStageRecursive(edge.DefinedModuleId, edges, stages, computed, currentPath);
                maxDependentStage = Math.Max(maxDependentStage, dependentStage);
            }

            stages[moduleId] = Math.Max(0, maxDependentStage + 1);
        }

        currentPath.Remove(moduleId);
        computed.Add(moduleId);
        return stages[moduleId];
    }

    /// <summary>
    /// Gets all dependencies that recursively depend on modules within the given namespace
    /// Also includes dependencies where modules within the namespace depend on modules in other namespaces
    /// Returns distinct dependency edges for namespace-level dependency analysis
    /// </summary>
    public async Task<List<Dependency>> ListRecursiveDestroyDependenciesForNamespace(Guid namespaceId)
    {
        // Get all dependencies where the namespace is involved (either as defined or referenced namespace)
        var recursiveDependencies = await _dbContext.RecursiveDestroyDependencies
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
    /// Builds a destroy dependency graph for a single module, showing all modules that depend on it
    /// </summary>
    public async Task<ModuleDependencyGraphDto> BuildModuleDestroyGraphAsync(Guid moduleId, string moduleName)
    {
        var dependencies = await ListRecursiveDestroyDependenciesForReferencedModule(moduleId);

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

        // Add root module if it's not in the lookup (case where nothing depends on it)
        if (!moduleInfoLookup.ContainsKey(moduleId))
        {
            var rootModule = await _dbContext.Modules
                .Include(m => m.Namespace)
                .ThenInclude(n => n.Stack)
                .FirstOrDefaultAsync(m => m.Id == moduleId);

            if (rootModule != null)
            {
                // Additionally add root module
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

            var nodeState = CreateNodeState(id, moduleInfo, DesiredStateHeadline.Destroyed);

            // For destroy, incoming edges are modules this depends on (referenced where this is defined)
            var incomingEdges = dependencies
                .Where(d => d.DefinedModuleId == id)
                .Select(d => new DependencyGraphEdgeDto
                {
                    ModuleId = d.ReferencedModuleId,
                    DisplayName = d.ReferencedDisplayName,
                    NamespaceId = d.ReferencedNamespaceId
                })
                .Distinct()
                .ToList();
            nodeState.IncomingEdges.AddRange(incomingEdges);

            // For destroy, outgoing edges are modules that depend on this (defined where this is referenced)
            var outgoingEdges = dependencies
                .Where(d => d.ReferencedModuleId == id)
                .Select(d => new DependencyGraphEdgeDto
                {
                    ModuleId = d.DefinedModuleId,
                    DisplayName = d.DefinedDisplayName,
                    NamespaceId = d.DefinedNamespaceId
                })
                .Distinct()
                .ToList();
            nodeState.OutgoingEdges.AddRange(outgoingEdges);

            nodeStates.Add(nodeState);
        }

        // Calculate stages and apply visual states
        CalculateDestroyStagesForNodes(nodeStates, dependencies);

        return new ModuleDependencyGraphDto
        {
            ModuleId = moduleId,
            ModuleName = moduleName,
            Direction = "Destroy",
            TargetState = DesiredStateHeadline.Destroyed,
            NodeStates = nodeStates
        };
    }

    /// <summary>
    /// Builds a destroy dependency graph for all modules within a namespace
    /// </summary>
    public async Task<NamespaceDependencyGraphDto> BuildNamespaceDestroyGraphAsync(Guid namespaceId, string namespaceName)
    {
        var dependencies = await ListRecursiveDestroyDependenciesForNamespace(namespaceId);

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

            var nodeState = CreateNodeState(moduleId, moduleInfo, DesiredStateHeadline.Destroyed);

            // Get incoming edges (dependencies) - what depends on this module (ReferencedModuleId = this module)
            var incomingEdges = dependencies.Where(e => e.ReferencedModuleId == moduleId).ToList();
            nodeState.IncomingEdges = incomingEdges
                .GroupBy(e => e.DefinedModuleId)
                .Select(g => g.First())
                .Select(e => new DependencyGraphEdgeDto
                {
                    DisplayName = e.DefinedDisplayName,
                    ModuleId = e.DefinedModuleId,
                    NamespaceId = e.DefinedNamespaceId
                })
                .ToList();

            // Get outgoing edges (dependents) - what this module depends on (DefinedModuleId = this module)
            var outgoingEdges = dependencies.Where(e => e.DefinedModuleId == moduleId).ToList();
            nodeState.OutgoingEdges = outgoingEdges
                .GroupBy(e => e.ReferencedModuleId)
                .Select(g => g.First())
                .Select(e => new DependencyGraphEdgeDto
                {
                    DisplayName = e.ReferencedDisplayName,
                    ModuleId = e.ReferencedModuleId,
                    NamespaceId = e.ReferencedNamespaceId
                })
                .ToList();

            nodeStates.Add(nodeState);
        }

        // Calculate stages using proper Destroy logic
        CalculateDestroyStagesForNodes(nodeStates, dependencies);

        return new NamespaceDependencyGraphDto
        {
            NamespaceId = namespaceId,
            NamespaceName = namespaceName,
            Direction = "Destroy",
            TargetState = DesiredStateHeadline.Destroyed,
            NodeStates = nodeStates
        };
    }

    private void CalculateDestroyStagesForNodes(List<DependencyGraphNodeStateDto> nodeStates, List<Dependency> dependencies)
    {
        var stages = new Dictionary<Guid, int>();
        var computed = new HashSet<Guid>();

        foreach (var nodeState in nodeStates)
            if (!computed.Contains(nodeState.ModuleId))
                CalculateDestroyStageRecursiveForNodes(nodeState.ModuleId, dependencies, stages, computed, new HashSet<Guid>());

        // Assign calculated stages (1-indexed for UI display)
        foreach (var nodeState in nodeStates)
        {
            nodeState.Stage = stages.GetValueOrDefault(nodeState.ModuleId, 0) + 1;
            nodeState.StageOrdinal = GetOrdinalSuffix(nodeState.Stage);
        }
    }

    private int CalculateDestroyStageRecursiveForNodes(Guid moduleId, List<Dependency> dependencies, Dictionary<Guid, int> stages, HashSet<Guid> computed, HashSet<Guid> currentPath)
    {
        if (computed.Contains(moduleId)) return stages[moduleId];

        if (currentPath.Contains(moduleId))
        {
            // Circular dependency detected - assign stage 0
            stages[moduleId] = 0;
            computed.Add(moduleId);
            return 0;
        }

        currentPath.Add(moduleId);

        // For destruction, we need to destroy dependent modules first
        // So find modules that depend on this module (where this module is ReferencedModule)
        var dependentEdges = dependencies.Where(e => e.ReferencedModuleId == moduleId).ToList();

        if (!dependentEdges.Any())
        {
            // No dependent modules, can be destroyed first (stage 0)
            stages[moduleId] = 0;
        }
        else
        {
            // Calculate stage as max dependent stage + 1
            var maxDependentStage = -1;
            foreach (var edge in dependentEdges)
            {
                var dependentStage = CalculateDestroyStageRecursiveForNodes(edge.DefinedModuleId, dependencies, stages, computed, currentPath);
                maxDependentStage = Math.Max(maxDependentStage, dependentStage);
            }

            stages[moduleId] = Math.Max(0, maxDependentStage + 1);
        }

        currentPath.Remove(moduleId);
        computed.Add(moduleId);
        return stages[moduleId];
    }

    /// <summary>
    /// Builds a destroy dependency graph for all modules within a stack
    /// </summary>
    public async Task<StackDependencyGraphDto> BuildStackDestroyGraphAsync(Guid stackId, string stackName)
    {
        var directDependencyService = new DependencyGraphService(_dbContext);
        var dependencies = await directDependencyService.ListForReferencedStack(stackId);

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

            var nodeState = CreateNodeState(moduleId, moduleInfo, DesiredStateHeadline.Destroyed);

            // For Destroy logic: IncomingEdges = what depends on this module (must be destroyed first)
            var incomingEdges = dependencies.Where(e => e.ReferencedModuleId == moduleId).ToList();
            nodeState.IncomingEdges = incomingEdges
                .GroupBy(e => e.DefinedModuleId)
                .Select(g => g.First())
                .Select(e => new DependencyGraphEdgeDto
                {
                    DisplayName = e.DefinedDisplayName,
                    ModuleId = e.DefinedModuleId,
                    NamespaceId = e.DefinedNamespaceId
                })
                .ToList();

            // For Destroy logic: OutgoingEdges = what this module depends on (destroyed after dependents)
            var outgoingEdges = dependencies.Where(e => e.DefinedModuleId == moduleId).ToList();
            nodeState.OutgoingEdges = outgoingEdges
                .GroupBy(e => e.ReferencedModuleId)
                .Select(g => g.First())
                .Select(e => new DependencyGraphEdgeDto
                {
                    DisplayName = e.ReferencedDisplayName,
                    ModuleId = e.ReferencedModuleId,
                    NamespaceId = e.ReferencedNamespaceId
                })
                .ToList();

            nodeStates.Add(nodeState);
        }

        // Calculate stages using proper Destroy logic (dependents destroyed before dependencies)
        CalculateDestroyStagesForNodes(nodeStates, dependencies);

        return new StackDependencyGraphDto
        {
            StackId = stackId,
            StackName = stackName,
            Direction = "Destroy",
            TargetState = DesiredStateHeadline.Destroyed,
            NodeStates = nodeStates
        };
    }


    public void Dispose()
    {
        _dbContext.Dispose();
    }
}