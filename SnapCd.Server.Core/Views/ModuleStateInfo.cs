// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Views;


public class ModuleStateInfo
{
    // Identity fields
    public Guid ModuleId { get; set; }
    public string Name { get; set; } = null!;
    public string NamespaceName { get; set; } = null!;
    public Guid NamespaceId { get; set; }
    public string StackName { get; set; } = null!;
    public Guid StackId { get; set; }
    public string DisplayName { get; set; } = null!;

    // State fields
    public ActualStateHeadline? LatestActualState { get; set; }
    public DesiredStateHeadline? DesiredState { get; set; }
    public DesiredStateHeadline? RunningDesiredState { get; set; }
    public DesiredStateHeadline? QueuedDesiredState { get; set; }
    public ExecutionStatus LatestExecutionStatus { get; set; }
    public bool IsRunning { get; set; }
    public bool IsQueued { get; set; }
}