// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Dtos;

public class DependencyGraphNodeStateDto
{
    public Guid ModuleId { get; set; }
    public Guid NamespaceId { get; set; }
    public string DisplayName { get; set; } = null!;
    public ActualStateHeadline? LatestActualState { get; set; } // The actual state from the last completed job (used when a job is running)
    public DesiredStateHeadline? DesiredState { get; set; } // The module's actual desired state from ModuleSaga
    public DesiredStateHeadline? QueuedDesiredState { get; set; } // The module's queued desired state from ModuleSaga
    public DesiredStateHeadline? RunningDesiredState { get; set; } // The desired state if a job is currently running
    public bool IsRunning { get; set; }
    public bool IsQueued { get; set; }
    public DesiredStateHeadline TargetState { get; set; } // The target state for this dependency graph view
    public int Stage { get; set; }

    // Dependencies - list of edge objects with display name and namespace info
    public List<DependencyGraphEdgeDto> IncomingEdges { get; set; } = new();
    public List<DependencyGraphEdgeDto> OutgoingEdges { get; set; } = new();

    // Calculate if current state matches desired state
    public bool IsStateMatch =>
        (DesiredState == DesiredStateHeadline.Applied && LatestActualState == ActualStateHeadline.Applied) ||
        (DesiredState == DesiredStateHeadline.Destroyed && LatestActualState == ActualStateHeadline.Destroyed);

    // Pre-calculated state display text
    public string StateDisplayText { get; set; } = "Unknown";

    // Pre-calculated state color
    public MudBlazor.Color StateColor { get; set; } = MudBlazor.Color.Info;

    // Pre-calculated node border style
    public string NodeBorderStyle { get; set; } = "border-left: 4px solid var(--mud-palette-info);";

    // Pre-calculated state icon
    public string StateIcon { get; set; } = MudBlazor.Icons.Material.Filled.StopCircle;

    // Ordinal stage display (1st, 2nd, 3rd, etc.)
    public string StageOrdinal { get; set; } = "1st";

    // Module name extracted from DisplayName
    public string ModuleName { get; set; } = "";

    // Namespace display name (Stack/Namespace)
    public string NamespaceDisplayName { get; set; } = "";

    // Just the namespace name without stack prefix
    public string NamespaceName
    {
        get
        {
            var parts = NamespaceDisplayName.Split('/');
            return parts.Length >= 2 ? parts[1] : NamespaceDisplayName;
        }
    }
}