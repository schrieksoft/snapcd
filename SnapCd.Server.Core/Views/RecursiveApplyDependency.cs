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

public class RecursiveApplyDependency : IRecursiveDependency
{
    // Root module details (the module that has dependencies - preserved throughout recursion)
    public Guid RootModuleId { get; set; }
    public Guid RootOrganizationId { get; set; }
    public string RootModuleName { get; set; } = null!;
    public Guid RootNamespaceId { get; set; }
    public string RootNamespaceName { get; set; } = null!;
    public Guid RootStackId { get; set; }
    public string RootStackName { get; set; } = null!;
    public string RootDisplayName { get; set; } = null!;
    public ActualStateHeadline? RootLatestActualState { get; set; }
    public DesiredStateHeadline? RootDesiredState { get; set; }
    public DesiredStateHeadline? RootQueuedDesiredState { get; set; }
    public bool RootIsRunning { get; set; }
    public bool RootIsQueued { get; set; }
    public DesiredStateHeadline? RootRunningDesiredState { get; set; }

    // Current edge: Defined Module (source of edge)
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

    // Current edge: Referenced Module (target of edge)
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

    // Depth in the recursive chain
    public int Depth { get; set; }

    // Navigation properties
    public Module DefinedModule { get; set; } = null!;
    public Module ReferencedModule { get; set; } = null!;
    public Namespace DefinedNamespace { get; set; } = null!;
    public Namespace ReferencedNamespace { get; set; } = null!;
    public Stack DefinedStack { get; set; } = null!;
    public Stack ReferencedStack { get; set; } = null!;
}