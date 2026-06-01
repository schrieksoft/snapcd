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

namespace SnapCd.Server.Core.Views.Interfaces;

public interface IRecursiveDependency
{
    // Defined Module (source of edge)
    Guid DefinedModuleId { get; }
    string DefinedModuleName { get; }
    Guid DefinedNamespaceId { get; }
    string DefinedNamespaceName { get; }
    Guid DefinedStackId { get; }
    string DefinedStackName { get; }
    string DefinedDisplayName { get; }
    ActualStateHeadline? DefinedLatestActualState { get; }
    DesiredStateHeadline? DefinedDesiredState { get; }
    DesiredStateHeadline? DefinedQueuedDesiredState { get; }
    bool DefinedIsRunning { get; }
    bool DefinedIsQueued { get; }
    DesiredStateHeadline? DefinedRunningDesiredState { get; }

    // Referenced Module (target of edge)
    Guid ReferencedModuleId { get; }
    string ReferencedModuleName { get; }
    Guid ReferencedNamespaceId { get; }
    string ReferencedNamespaceName { get; }
    Guid ReferencedStackId { get; }
    string ReferencedStackName { get; }
    string ReferencedDisplayName { get; }
    ActualStateHeadline? ReferencedLatestActualState { get; }
    DesiredStateHeadline? ReferencedDesiredState { get; }
    DesiredStateHeadline? ReferencedQueuedDesiredState { get; }
    bool ReferencedIsRunning { get; }
    bool ReferencedIsQueued { get; }
    DesiredStateHeadline? ReferencedRunningDesiredState { get; }

    // Navigation properties
    Module DefinedModule { get; }
    Module ReferencedModule { get; }
    Namespace DefinedNamespace { get; }
    Namespace ReferencedNamespace { get; }
    Stack DefinedStack { get; }
    Stack ReferencedStack { get; }
}
