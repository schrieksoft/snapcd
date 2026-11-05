// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Services.Nonsecured.DependencyGraph;

/// <summary>
/// The dependency graph's reads, as composable queries over the trigger-maintained tables.
///
/// These return IQueryable so a caller can filter before the query runs, the way it would
/// against a DbSet. Each replaces a view of the same name; the two are held to the same rows
/// by ViewParityTests until the view is deleted.
/// </summary>
public static class DependencyGraphQueries
{
    /// <summary>
    /// Every dependency edge with both ends named, plus one row per Module that is on neither
    /// side of any edge. Replaces vw_Dependencies.
    /// </summary>
    public static IQueryable<Dependency> Dependencies(SnapCdDbContext db)
    {
        var edges =
            from e in db.DependencyEdges
            join dm in db.Modules on e.DefinedModuleId equals dm.Id
            join dns in db.Namespaces on dm.NamespaceId equals dns.Id
            join dst in db.Stacks on dns.StackId equals dst.Id
            join rm in db.Modules on e.ReferencedModuleId equals rm.Id
            join rns in db.Namespaces on rm.NamespaceId equals rns.Id
            join rst in db.Stacks on rns.StackId equals rst.Id
            from ds in db.ModuleStates.Where(x => x.ModuleId == e.DefinedModuleId).DefaultIfEmpty()
            from rs in db.ModuleStates.Where(x => x.ModuleId == e.ReferencedModuleId).DefaultIfEmpty()
            from dj in db.ModuleJobs.Where(j => j.Id == ds.CurrentModuleJobId).DefaultIfEmpty()
            from rj in db.ModuleJobs.Where(j => j.Id == rs.CurrentModuleJobId).DefaultIfEmpty()
            select new Dependency
            {
                DefinedModuleId = e.DefinedModuleId,
                DefinedOrganizationId = dm.OrganizationId,
                DefinedModuleName = dm.Name,
                DefinedNamespaceId = dm.NamespaceId,
                DefinedNamespaceName = dns.Name,
                DefinedStackId = dns.StackId,
                DefinedStackName = dst.Name,
                DefinedDisplayName = dst.Name + "/" + dns.Name + "/" + dm.Name,
                DefinedLatestActualState = ds.LatestActualStateHeadline,
                DefinedDesiredState = ds.DesiredStateHeadline,
                DefinedQueuedDesiredState = ds.QueuedDesiredStateHeadline,
                DefinedIsRunning = ds.IsRunning,
                DefinedIsQueued = ds.QueuedDesiredStateHeadline != null,
                DefinedRunningDesiredState = ds.IsRunning ? ds.DesiredStateHeadline : null,
                DefinedCurrentModuleJobId = ds.CurrentModuleJobId,
                DefinedWaitingForApproval = dj.WaitingForApproval == true,

                ReferencedModuleId = e.ReferencedModuleId,
                ReferencedOrganizationId = rm.OrganizationId,
                ReferencedModuleName = rm.Name,
                ReferencedNamespaceId = rm.NamespaceId,
                ReferencedNamespaceName = rns.Name,
                ReferencedStackId = rns.StackId,
                ReferencedStackName = rst.Name,
                ReferencedDisplayName = rst.Name + "/" + rns.Name + "/" + rm.Name,
                ReferencedLatestActualState = rs.LatestActualStateHeadline,
                ReferencedDesiredState = rs.DesiredStateHeadline,
                ReferencedQueuedDesiredState = rs.QueuedDesiredStateHeadline,
                ReferencedIsRunning = rs.IsRunning,
                ReferencedIsQueued = rs.QueuedDesiredStateHeadline != null,
                ReferencedRunningDesiredState = rs.IsRunning ? rs.DesiredStateHeadline : null,
                ReferencedCurrentModuleJobId = rs.CurrentModuleJobId,
                ReferencedWaitingForApproval = rj.WaitingForApproval == true
            };

        // A Module on neither side of any edge would otherwise not appear at all.
        var standalone =
            from m in db.Modules
            join ns in db.Namespaces on m.NamespaceId equals ns.Id
            join st in db.Stacks on ns.StackId equals st.Id
            from ms in db.ModuleStates.Where(x => x.ModuleId == m.Id).DefaultIfEmpty()
            from mj in db.ModuleJobs.Where(j => j.Id == ms.CurrentModuleJobId).DefaultIfEmpty()
            where !db.DependencyEdges.Any(d => d.DefinedModuleId == m.Id)
               && !db.DependencyEdges.Any(d => d.ReferencedModuleId == m.Id)
            select new Dependency
            {
                DefinedModuleId = m.Id,
                DefinedOrganizationId = m.OrganizationId,
                DefinedModuleName = m.Name,
                DefinedNamespaceId = ns.Id,
                DefinedNamespaceName = ns.Name,
                DefinedStackId = st.Id,
                DefinedStackName = st.Name,
                DefinedDisplayName = st.Name + "/" + ns.Name + "/" + m.Name,
                DefinedLatestActualState = ms.LatestActualStateHeadline,
                DefinedDesiredState = ms.DesiredStateHeadline,
                DefinedQueuedDesiredState = ms.QueuedDesiredStateHeadline,
                DefinedIsRunning = ms.IsRunning,
                DefinedIsQueued = ms.QueuedDesiredStateHeadline != null,
                DefinedRunningDesiredState = ms.IsRunning ? ms.DesiredStateHeadline : null,
                DefinedCurrentModuleJobId = ms.CurrentModuleJobId,
                DefinedWaitingForApproval = mj.WaitingForApproval == true,

                ReferencedModuleId = null,
                ReferencedOrganizationId = null,
                ReferencedModuleName = null,
                ReferencedNamespaceId = null,
                ReferencedNamespaceName = null,
                ReferencedStackId = null,
                ReferencedStackName = null,
                ReferencedDisplayName = null,
                ReferencedLatestActualState = null,
                ReferencedDesiredState = null,
                ReferencedQueuedDesiredState = null,
                ReferencedIsRunning = null,
                ReferencedIsQueued = null,
                ReferencedRunningDesiredState = null,
                ReferencedCurrentModuleJobId = null,
                ReferencedWaitingForApproval = null
            };

        return edges.Distinct().Concat(standalone);
    }

    /// <summary>
    /// The apply closure: every pair the graph can reach from a root, with the state of
    /// all three ends, plus one row per Module that no edge touches. Replaces
    /// vw_RecursiveApplyDependencies.
    /// </summary>
    public static IQueryable<RecursiveApplyDependency> RecursiveApply(SnapCdDbContext db)
    {
        var edges =
            from re in db.RecursiveDependencyEdges.Where(x => x.Direction == 1)
            from rs in db.ModuleStates.Where(x => x.ModuleId == re.RootModuleId).DefaultIfEmpty()
            from ds in db.ModuleStates.Where(x => x.ModuleId == re.DefinedModuleId).DefaultIfEmpty()
            from xs in db.ModuleStates.Where(x => x.ModuleId == re.ReferencedModuleId).DefaultIfEmpty()
            from dj in db.ModuleJobs.Where(j => j.Id == ds.CurrentModuleJobId).DefaultIfEmpty()
            from xj in db.ModuleJobs.Where(j => j.Id == xs.CurrentModuleJobId).DefaultIfEmpty()
            select new RecursiveApplyDependency
            {
                RootModuleId = re.RootModuleId,
                RootOrganizationId = re.RootOrganizationId,
                RootModuleName = re.RootModuleName,
                RootNamespaceId = re.RootNamespaceId,
                RootNamespaceName = re.RootNamespaceName,
                RootStackId = re.RootStackId,
                RootStackName = re.RootStackName,
                RootDisplayName = re.RootDisplayName,
                RootLatestActualState = rs.LatestActualStateHeadline,
                RootDesiredState = rs.DesiredStateHeadline,
                RootQueuedDesiredState = rs.QueuedDesiredStateHeadline,
                RootIsRunning = rs.IsRunning,
                RootIsQueued = rs.QueuedDesiredStateHeadline != null,
                RootRunningDesiredState = rs.IsRunning ? rs.DesiredStateHeadline : null,

                DefinedModuleId = re.DefinedModuleId,
                DefinedOrganizationId = re.DefinedOrganizationId,
                DefinedModuleName = re.DefinedModuleName,
                DefinedNamespaceId = re.DefinedNamespaceId,
                DefinedNamespaceName = re.DefinedNamespaceName,
                DefinedStackId = re.DefinedStackId,
                DefinedStackName = re.DefinedStackName,
                DefinedDisplayName = re.DefinedDisplayName,
                DefinedLatestActualState = ds.LatestActualStateHeadline,
                DefinedDesiredState = ds.DesiredStateHeadline,
                DefinedQueuedDesiredState = ds.QueuedDesiredStateHeadline,
                DefinedIsRunning = ds.IsRunning,
                DefinedIsQueued = ds.QueuedDesiredStateHeadline != null,
                DefinedRunningDesiredState = ds.IsRunning ? ds.DesiredStateHeadline : null,
                DefinedCurrentModuleJobId = ds.CurrentModuleJobId,
                DefinedWaitingForApproval = dj.WaitingForApproval == true,

                ReferencedModuleId = re.ReferencedModuleId,
                ReferencedOrganizationId = re.ReferencedOrganizationId,
                ReferencedModuleName = re.ReferencedModuleName,
                ReferencedNamespaceId = re.ReferencedNamespaceId,
                ReferencedNamespaceName = re.ReferencedNamespaceName,
                ReferencedStackId = re.ReferencedStackId,
                ReferencedStackName = re.ReferencedStackName,
                ReferencedDisplayName = re.ReferencedDisplayName,
                ReferencedLatestActualState = xs.LatestActualStateHeadline,
                ReferencedDesiredState = xs.DesiredStateHeadline,
                ReferencedQueuedDesiredState = xs.QueuedDesiredStateHeadline,
                ReferencedIsRunning = xs.IsRunning,
                ReferencedIsQueued = xs.QueuedDesiredStateHeadline != null,
                ReferencedRunningDesiredState = xs.IsRunning ? xs.DesiredStateHeadline : null,
                ReferencedCurrentModuleJobId = xs.CurrentModuleJobId,
                ReferencedWaitingForApproval = xj.WaitingForApproval == true,

                Depth = re.Depth
            };

        var standalone =
            from m in db.Modules
            join ns in db.Namespaces on m.NamespaceId equals ns.Id
            join st in db.Stacks on ns.StackId equals st.Id
            from ms in db.ModuleStates.Where(x => x.ModuleId == m.Id).DefaultIfEmpty()
            from mj in db.ModuleJobs.Where(j => j.Id == ms.CurrentModuleJobId).DefaultIfEmpty()
            where !db.DependencyEdges.Any(d => d.DefinedModuleId == m.Id)
               && !db.DependencyEdges.Any(d => d.ReferencedModuleId == m.Id)
            select new RecursiveApplyDependency
            {
                RootModuleId = m.Id,
                RootOrganizationId = m.OrganizationId,
                RootModuleName = m.Name,
                RootNamespaceId = ns.Id,
                RootNamespaceName = ns.Name,
                RootStackId = st.Id,
                RootStackName = st.Name,
                RootDisplayName = st.Name + "/" + ns.Name + "/" + m.Name,
                RootLatestActualState = ms.LatestActualStateHeadline,
                RootDesiredState = ms.DesiredStateHeadline,
                RootQueuedDesiredState = ms.QueuedDesiredStateHeadline,
                RootIsRunning = ms.IsRunning,
                RootIsQueued = ms.QueuedDesiredStateHeadline != null,
                RootRunningDesiredState = ms.IsRunning ? ms.DesiredStateHeadline : null,

                DefinedModuleId = m.Id,
                DefinedOrganizationId = m.OrganizationId,
                DefinedModuleName = m.Name,
                DefinedNamespaceId = ns.Id,
                DefinedNamespaceName = ns.Name,
                DefinedStackId = st.Id,
                DefinedStackName = st.Name,
                DefinedDisplayName = st.Name + "/" + ns.Name + "/" + m.Name,
                DefinedLatestActualState = ms.LatestActualStateHeadline,
                DefinedDesiredState = ms.DesiredStateHeadline,
                DefinedQueuedDesiredState = ms.QueuedDesiredStateHeadline,
                DefinedIsRunning = ms.IsRunning,
                DefinedIsQueued = ms.QueuedDesiredStateHeadline != null,
                DefinedRunningDesiredState = ms.IsRunning ? ms.DesiredStateHeadline : null,
                DefinedCurrentModuleJobId = ms.CurrentModuleJobId,
                DefinedWaitingForApproval = mj.WaitingForApproval == true,

                ReferencedModuleId = null,
                ReferencedOrganizationId = null,
                ReferencedModuleName = null,
                ReferencedNamespaceId = null,
                ReferencedNamespaceName = null,
                ReferencedStackId = null,
                ReferencedStackName = null,
                ReferencedDisplayName = null,
                ReferencedLatestActualState = null,
                ReferencedDesiredState = null,
                ReferencedQueuedDesiredState = null,
                ReferencedIsRunning = null,
                ReferencedIsQueued = null,
                ReferencedRunningDesiredState = null,
                ReferencedCurrentModuleJobId = null,
                ReferencedWaitingForApproval = null,

                Depth = 0
            };

        return edges.Concat(standalone);
    }

    /// <summary>
    /// The destroy closure: every pair the graph can reach from a root, with the state of
    /// all three ends, plus one row per Module that no edge touches. Replaces
    /// vw_RecursiveDestroyDependencies.
    /// </summary>
    public static IQueryable<RecursiveDestroyDependency> RecursiveDestroy(SnapCdDbContext db)
    {
        var edges =
            from re in db.RecursiveDependencyEdges.Where(x => x.Direction == 2)
            from rs in db.ModuleStates.Where(x => x.ModuleId == re.RootModuleId).DefaultIfEmpty()
            from ds in db.ModuleStates.Where(x => x.ModuleId == re.DefinedModuleId).DefaultIfEmpty()
            from xs in db.ModuleStates.Where(x => x.ModuleId == re.ReferencedModuleId).DefaultIfEmpty()
            from dj in db.ModuleJobs.Where(j => j.Id == ds.CurrentModuleJobId).DefaultIfEmpty()
            from xj in db.ModuleJobs.Where(j => j.Id == xs.CurrentModuleJobId).DefaultIfEmpty()
            select new RecursiveDestroyDependency
            {
                RootModuleId = re.RootModuleId,
                RootOrganizationId = re.RootOrganizationId,
                RootModuleName = re.RootModuleName,
                RootNamespaceId = re.RootNamespaceId,
                RootNamespaceName = re.RootNamespaceName,
                RootStackId = re.RootStackId,
                RootStackName = re.RootStackName,
                RootDisplayName = re.RootDisplayName,
                RootLatestActualState = rs.LatestActualStateHeadline,
                RootDesiredState = rs.DesiredStateHeadline,
                RootQueuedDesiredState = rs.QueuedDesiredStateHeadline,
                RootIsRunning = rs.IsRunning,
                RootIsQueued = rs.QueuedDesiredStateHeadline != null,
                RootRunningDesiredState = rs.IsRunning ? rs.DesiredStateHeadline : null,

                DefinedModuleId = re.DefinedModuleId,
                DefinedOrganizationId = re.DefinedOrganizationId,
                DefinedModuleName = re.DefinedModuleName,
                DefinedNamespaceId = re.DefinedNamespaceId,
                DefinedNamespaceName = re.DefinedNamespaceName,
                DefinedStackId = re.DefinedStackId,
                DefinedStackName = re.DefinedStackName,
                DefinedDisplayName = re.DefinedDisplayName,
                DefinedLatestActualState = ds.LatestActualStateHeadline,
                DefinedDesiredState = ds.DesiredStateHeadline,
                DefinedQueuedDesiredState = ds.QueuedDesiredStateHeadline,
                DefinedIsRunning = ds.IsRunning,
                DefinedIsQueued = ds.QueuedDesiredStateHeadline != null,
                DefinedRunningDesiredState = ds.IsRunning ? ds.DesiredStateHeadline : null,
                DefinedCurrentModuleJobId = ds.CurrentModuleJobId,
                DefinedWaitingForApproval = dj.WaitingForApproval == true,

                ReferencedModuleId = re.ReferencedModuleId,
                ReferencedOrganizationId = re.ReferencedOrganizationId,
                ReferencedModuleName = re.ReferencedModuleName,
                ReferencedNamespaceId = re.ReferencedNamespaceId,
                ReferencedNamespaceName = re.ReferencedNamespaceName,
                ReferencedStackId = re.ReferencedStackId,
                ReferencedStackName = re.ReferencedStackName,
                ReferencedDisplayName = re.ReferencedDisplayName,
                ReferencedLatestActualState = xs.LatestActualStateHeadline,
                ReferencedDesiredState = xs.DesiredStateHeadline,
                ReferencedQueuedDesiredState = xs.QueuedDesiredStateHeadline,
                ReferencedIsRunning = xs.IsRunning,
                ReferencedIsQueued = xs.QueuedDesiredStateHeadline != null,
                ReferencedRunningDesiredState = xs.IsRunning ? xs.DesiredStateHeadline : null,
                ReferencedCurrentModuleJobId = xs.CurrentModuleJobId,
                ReferencedWaitingForApproval = xj.WaitingForApproval == true,

                Depth = re.Depth
            };

        var standalone =
            from m in db.Modules
            join ns in db.Namespaces on m.NamespaceId equals ns.Id
            join st in db.Stacks on ns.StackId equals st.Id
            from ms in db.ModuleStates.Where(x => x.ModuleId == m.Id).DefaultIfEmpty()
            from mj in db.ModuleJobs.Where(j => j.Id == ms.CurrentModuleJobId).DefaultIfEmpty()
            where !db.DependencyEdges.Any(d => d.DefinedModuleId == m.Id)
               && !db.DependencyEdges.Any(d => d.ReferencedModuleId == m.Id)
            select new RecursiveDestroyDependency
            {
                RootModuleId = m.Id,
                RootOrganizationId = m.OrganizationId,
                RootModuleName = m.Name,
                RootNamespaceId = ns.Id,
                RootNamespaceName = ns.Name,
                RootStackId = st.Id,
                RootStackName = st.Name,
                RootDisplayName = st.Name + "/" + ns.Name + "/" + m.Name,
                RootLatestActualState = ms.LatestActualStateHeadline,
                RootDesiredState = ms.DesiredStateHeadline,
                RootQueuedDesiredState = ms.QueuedDesiredStateHeadline,
                RootIsRunning = ms.IsRunning,
                RootIsQueued = ms.QueuedDesiredStateHeadline != null,
                RootRunningDesiredState = ms.IsRunning ? ms.DesiredStateHeadline : null,

                DefinedModuleId = m.Id,
                DefinedOrganizationId = m.OrganizationId,
                DefinedModuleName = m.Name,
                DefinedNamespaceId = ns.Id,
                DefinedNamespaceName = ns.Name,
                DefinedStackId = st.Id,
                DefinedStackName = st.Name,
                DefinedDisplayName = st.Name + "/" + ns.Name + "/" + m.Name,
                DefinedLatestActualState = ms.LatestActualStateHeadline,
                DefinedDesiredState = ms.DesiredStateHeadline,
                DefinedQueuedDesiredState = ms.QueuedDesiredStateHeadline,
                DefinedIsRunning = ms.IsRunning,
                DefinedIsQueued = ms.QueuedDesiredStateHeadline != null,
                DefinedRunningDesiredState = ms.IsRunning ? ms.DesiredStateHeadline : null,
                DefinedCurrentModuleJobId = ms.CurrentModuleJobId,
                DefinedWaitingForApproval = mj.WaitingForApproval == true,

                ReferencedModuleId = null,
                ReferencedOrganizationId = null,
                ReferencedModuleName = null,
                ReferencedNamespaceId = null,
                ReferencedNamespaceName = null,
                ReferencedStackId = null,
                ReferencedStackName = null,
                ReferencedDisplayName = null,
                ReferencedLatestActualState = null,
                ReferencedDesiredState = null,
                ReferencedQueuedDesiredState = null,
                ReferencedIsRunning = null,
                ReferencedIsQueued = null,
                ReferencedRunningDesiredState = null,
                ReferencedCurrentModuleJobId = null,
                ReferencedWaitingForApproval = null,

                Depth = 0
            };

        return edges.Concat(standalone);
    }
}
