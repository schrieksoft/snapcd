// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Services.DependencyGraph;

public class DependencyGraphServiceFactory(IDbContextFactory<SnapCdDbContext> dbContextFactory)
{
    public DependencyGraphService Create()
    {
        return new DependencyGraphService(dbContextFactory.CreateDbContext());
    }
}

public class DependencyGraphService : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public DependencyGraphService(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Dependency>> ListForDefinedModule(Guid moduleId)
    {
        return await _dbContext.Dependencies
            .Where(e => e.DefinedModuleId == moduleId)
            .ToListAsync();
    }

    public async Task<List<Dependency>> ListForReferencedModule(Guid moduleId)
    {
        return await _dbContext.Dependencies
            .Where(e => e.ReferencedModuleId == moduleId)
            .ToListAsync();
    }


    public async Task<List<Guid>> ListModuleIdsForDefinedModule(Guid moduleId)
    {
        return await _dbContext.Dependencies
            .Where(e => e.DefinedModuleId == moduleId && e.ReferencedModuleId.HasValue)
            .Select(x => x.ReferencedModuleId!.Value)
            .ToListAsync();
    }

    public async Task<List<Guid>> ListModuleIdsForReferencedModule(Guid moduleId)
    {
        return await _dbContext.Dependencies
            .Where(e => e.ReferencedModuleId == moduleId)
            .Select(x => x.DefinedModuleId)
            .ToListAsync();
    }


    public virtual async Task<List<Dependency>> ListForDefinedNamespace(Guid namespaceId)
    {
        return await _dbContext.Dependencies
            .Where(e => e.DefinedNamespaceId == namespaceId)
            .ToListAsync();
    }

    public virtual async Task<List<Dependency>> ListForReferencedNamespace(Guid namespaceId)
    {
        return await _dbContext.Dependencies
            .Where(e => e.ReferencedNamespaceId == namespaceId)
            .ToListAsync();
    }

    public virtual async Task<List<Dependency>> ListForDefinedStack(Guid stackId)
    {
        return await _dbContext.Dependencies
            .Where(e => e.DefinedStackId == stackId)
            .ToListAsync();
    }

    public virtual async Task<List<Dependency>> ListForReferencedStack(Guid stackId)
    {
        return await _dbContext.Dependencies
            .Where(e => e.ReferencedStackId == stackId)
            .ToListAsync();
    }

    public async Task<ModuleStateInfo?> GetModuleStateAsync(Guid moduleId)
    {
        // this method is meant to be used in order to update individual module state (e.g. on event triggering that must make a single node in a displayed dependency graph update)
        var query = from m in _dbContext.Modules
            join ns in _dbContext.Namespaces on m.NamespaceId equals ns.Id
            join st in _dbContext.Stacks on ns.StackId equals st.Id
            where m.Id == moduleId
            let currentJob = _dbContext.ModuleJobs
                .Where(mj => mj.ModuleId == m.Id && mj.IsCurrent == true)
                .OrderByDescending(mj => mj.TimestampStart)
                .FirstOrDefault()
            let latestCompletedJob = _dbContext.ModuleJobs
                .Where(mj => mj.ModuleId == m.Id && mj.TimestampEnd != null && mj.ActualStateHeadline != null)
                .OrderByDescending(mj => mj.TimestampEnd)
                .FirstOrDefault()
            let saga = _dbContext.ModuleSagas
                .Where(ms => ms.CorrelationId == m.Id)
                .FirstOrDefault()
            select new ModuleStateInfo
            {
                // Identity fields
                ModuleId = m.Id,
                Name = m.Name,
                NamespaceName = ns.Name,
                NamespaceId = ns.Id,
                StackName = st.Name,
                StackId = st.Id,
                DisplayName = st.Name + "/" + ns.Name + "/" + m.Name,
                // State fields
                LatestActualState = latestCompletedJob != null ? latestCompletedJob.ActualStateHeadline : null,
                DesiredState = saga != null ? saga.DesiredStateHeadline : null,
                RunningDesiredState = saga != null && currentJob != null ? currentJob.IsCurrent == true ? saga.DesiredStateHeadline : null : null,
                QueuedDesiredState = saga != null ? saga.QueuedDesiredStateHeadline : null,
                IsRunning = currentJob != null && currentJob.IsCurrent == true,
                IsQueued = saga != null && saga.QueuedDesiredStateHeadline != null,
                LatestExecutionStatus = currentJob != null ? currentJob.Status : ExecutionStatus.Unknown
            };

        return await query.FirstOrDefaultAsync();
    }



    public virtual void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}