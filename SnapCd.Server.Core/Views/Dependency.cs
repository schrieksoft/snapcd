// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Views.Interfaces;

namespace SnapCd.Server.Core.Views;

public class Dependency
{
    // Defined Module (source of edge)
    public Guid DefinedModuleId { get; set; }
    public Guid DefinedOrganizationId { get; set; }
    public string DefinedModuleName { get; set; } = null!;
    public Guid DefinedNamespaceId { get; set; }
    public string DefinedNamespaceName { get; set; } = null!;
    public Guid DefinedStackId { get; set; }
    public string DefinedStackName { get; set; } = null!;
    public string DefinedDisplayName { get; set; } = null!;
    public ActualStateHeadline? DefinedLatestActualState { get; set; }
    public DesiredStateHeadline? DefinedDesiredState { get; set; }
    public DesiredStateHeadline? DefinedQueuedDesiredState { get; set; }
    public bool DefinedIsRunning { get; set; }
    public bool DefinedIsQueued { get; set; }
    public DesiredStateHeadline? DefinedRunningDesiredState { get; set; }

    // Referenced Module (target of edge)
    public Guid ReferencedModuleId { get; set; }
    public Guid ReferencedOrganizationId { get; set; }
    public string ReferencedModuleName { get; set; } = null!;
    public Guid ReferencedNamespaceId { get; set; }
    public string ReferencedNamespaceName { get; set; } = null!;
    public Guid ReferencedStackId { get; set; }
    public string ReferencedStackName { get; set; } = null!;
    public string ReferencedDisplayName { get; set; } = null!;
    public ActualStateHeadline? ReferencedLatestActualState { get; set; }
    public DesiredStateHeadline? ReferencedDesiredState { get; set; }
    public DesiredStateHeadline? ReferencedQueuedDesiredState { get; set; }
    public bool ReferencedIsRunning { get; set; }
    public bool ReferencedIsQueued { get; set; }
    public DesiredStateHeadline? ReferencedRunningDesiredState { get; set; }

    // Navigation properties
    public Module DefinedModule { get; set; } = null!;
    public Module ReferencedModule { get; set; } = null!;
    public Namespace DefinedNamespace { get; set; } = null!;
    public Namespace ReferencedNamespace { get; set; } = null!;
    public Stack DefinedStack { get; set; } = null!;
    public Stack ReferencedStack { get; set; } = null!;

    // Factory methods for creating ModuleStateInfo from dependency edges
    public ModuleStateInfo ToDefinedModuleStateInfo() => new ModuleStateInfo
    {
        ModuleId = DefinedModuleId,
        Name = DefinedModuleName,
        NamespaceName = DefinedNamespaceName,
        NamespaceId = DefinedNamespaceId,
        StackName = DefinedStackName,
        StackId = DefinedStackId,
        DisplayName = DefinedDisplayName,
        LatestActualState = DefinedLatestActualState,
        DesiredState = DefinedDesiredState,
        QueuedDesiredState = DefinedQueuedDesiredState,
        IsRunning = DefinedIsRunning,
        IsQueued = DefinedIsQueued,
        RunningDesiredState = DefinedRunningDesiredState
    };

    public ModuleStateInfo ToReferencedModuleStateInfo() => new ModuleStateInfo
    {
        ModuleId = ReferencedModuleId,
        Name = ReferencedModuleName,
        NamespaceName = ReferencedNamespaceName,
        NamespaceId = ReferencedNamespaceId,
        StackName = ReferencedStackName,
        StackId = ReferencedStackId,
        DisplayName = ReferencedDisplayName,
        LatestActualState = ReferencedLatestActualState,
        DesiredState = ReferencedDesiredState,
        QueuedDesiredState = ReferencedQueuedDesiredState,
        IsRunning = ReferencedIsRunning,
        IsQueued = ReferencedIsQueued,
        RunningDesiredState = ReferencedRunningDesiredState
    };

    // Factory method to create a Dependency from a recursive dependency type
    public static Dependency FromRecursive(IRecursiveDependency rd) => new Dependency
    {
        DefinedModuleId = rd.DefinedModuleId,
        DefinedModuleName = rd.DefinedModuleName,
        DefinedNamespaceId = rd.DefinedNamespaceId,
        DefinedNamespaceName = rd.DefinedNamespaceName,
        DefinedStackId = rd.DefinedStackId,
        DefinedStackName = rd.DefinedStackName,
        DefinedDisplayName = rd.DefinedDisplayName,
        DefinedLatestActualState = rd.DefinedLatestActualState,
        DefinedDesiredState = rd.DefinedDesiredState,
        DefinedQueuedDesiredState = rd.DefinedQueuedDesiredState,
        DefinedIsRunning = rd.DefinedIsRunning,
        DefinedIsQueued = rd.DefinedIsQueued,
        DefinedRunningDesiredState = rd.DefinedRunningDesiredState,
        ReferencedModuleId = rd.ReferencedModuleId,
        ReferencedModuleName = rd.ReferencedModuleName,
        ReferencedNamespaceId = rd.ReferencedNamespaceId,
        ReferencedNamespaceName = rd.ReferencedNamespaceName,
        ReferencedStackId = rd.ReferencedStackId,
        ReferencedStackName = rd.ReferencedStackName,
        ReferencedDisplayName = rd.ReferencedDisplayName,
        ReferencedLatestActualState = rd.ReferencedLatestActualState,
        ReferencedDesiredState = rd.ReferencedDesiredState,
        ReferencedQueuedDesiredState = rd.ReferencedQueuedDesiredState,
        ReferencedIsRunning = rd.ReferencedIsRunning,
        ReferencedIsQueued = rd.ReferencedIsQueued,
        ReferencedRunningDesiredState = rd.ReferencedRunningDesiredState,
        DefinedModule = rd.DefinedModule,
        ReferencedModule = rd.ReferencedModule,
        DefinedNamespace = rd.DefinedNamespace,
        ReferencedNamespace = rd.ReferencedNamespace,
        DefinedStack = rd.DefinedStack,
        ReferencedStack = rd.ReferencedStack
    };
}