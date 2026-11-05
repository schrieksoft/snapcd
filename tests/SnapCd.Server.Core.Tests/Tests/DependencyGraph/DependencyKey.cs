// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Interfaces;

namespace SnapCd.Server.Core.Tests.Tests.DependencyGraph;

/// <summary>
/// A Dependency flattened to a comparable string, so two sources can be diffed as sets and a
/// mismatch names the row rather than reporting that two lists differ.
///
/// Every scalar column is included. The navigation properties are not: they are EF fixup rather
/// than anything the view returns, and loading them would compare the wrong thing.
/// </summary>
public static class DependencyKey
{
    public static string Of(Dependency d) => string.Join("|",
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
        d.DefinedCurrentModuleJobId,
        d.DefinedWaitingForApproval,
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
        d.ReferencedCurrentModuleJobId,
        d.ReferencedWaitingForApproval);

    /// <summary>
    /// Every scalar column of a recursive row, including the Root side and Depth.
    ///
    /// Dependency.FromRecursive drops both, so converting before comparing would leave fifteen
    /// Root columns and the Depth untested.
    /// </summary>
    public static string OfRecursive(IRecursiveDependency d) => string.Join("|",
        d.RootModuleId,
        d.RootOrganizationId,
        d.RootModuleName,
        d.RootNamespaceId,
        d.RootNamespaceName,
        d.RootStackId,
        d.RootStackName,
        d.RootDisplayName,
        d.RootLatestActualState,
        d.RootDesiredState,
        d.RootQueuedDesiredState,
        d.RootIsRunning,
        d.RootIsQueued,
        d.RootRunningDesiredState,
        d.Depth,
        Of(Dependency.FromRecursive(d)));
}
