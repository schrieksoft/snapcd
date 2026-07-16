// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.UI.Dashboard.Theme;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Services.DependencyGraph;

public abstract class ModuleGraphServiceBase
{
    public static string GetStateDisplayText(ActualStateHeadline? latestActualState, bool isRunning, bool isQueued, DesiredStateHeadline? runningDesiredState, DesiredStateHeadline? queuedDesiredState)
    {
        var actualDisplay = latestActualState?.ToString() ?? "Unknown";

        // If not running and not queued, just show actual
        if (!isRunning && !isQueued) return actualDisplay;

        // Rule 1: If running, always show "→ Applied/Destroyed (running)"
        if (isRunning)
        {
            // Also check if queued
            if (isQueued)
            {
                // Show both running and queued
                return $"{actualDisplay} → {runningDesiredState} (running) → {queuedDesiredState} (queued)";
            }
            else
            {
                // Only running, no queued
                return $"{actualDisplay} → {runningDesiredState} (running)";
            }
        }

        // Rule 2: If not running but queued, show only "→ Applied/Destroyed (queued)"
        if (isQueued)
        {
            return $"{actualDisplay} → {queuedDesiredState} (queued)";
        }

        // Rule 3: If neither running nor queued, show only current status
        return actualDisplay;
    }

    public static MudBlazor.Color GetStateColor(ActualStateHeadline? actualState, bool isRunning)
    {
        // If running, return yellow (Warning color)
        if (isRunning) return MudBlazor.Color.Warning;

        return actualState switch
        {
            ActualStateHeadline.Applied => MudBlazor.Color.Success,
            ActualStateHeadline.Destroyed => MudBlazor.Color.Info,
            ActualStateHeadline.None => MudBlazor.Color.Info,
            null => MudBlazor.Color.Info,
            _ => MudBlazor.Color.Error
        };
    }
    
    public static string GetStateIcon(ActualStateHeadline? actualState, bool isRunning)
    {
        // If running, show a sync/progress icon
        if (isRunning) return SnapCdIcons.PlayCircle;

        return actualState switch
        {
            ActualStateHeadline.Applied => SnapCdIcons.CheckCircle,
            ActualStateHeadline.Destroyed => SnapCdIcons.StopCircle,
            ActualStateHeadline.None => SnapCdIcons.StopCircle,
            null => SnapCdIcons.StopCircle,
            _ => SnapCdIcons.Error
        };
    }

    public static string GetNodeBorderStyle(ActualStateHeadline? actualState, bool isRunning)
    {
        // If running, use yellow/warning color
        if (isRunning) return "border-left: 4px solid var(--mud-palette-warning);";

        var borderColor = actualState switch
        {
            ActualStateHeadline.Applied => "var(--mud-palette-success)",
            ActualStateHeadline.Destroyed => "var(--mud-palette-info)",
            ActualStateHeadline.None => "var(--mud-palette-info)",
            null => "var(--mud-palette-info)",
            _ => "var(--mud-palette-error)"
        };
        return $"border-left: 4px solid {borderColor};";
    }


    /// <summary>
    /// Updates a DependencyGraphNodeStateDto with state from a ModuleStateInfo
    /// </summary>
    public static void UpdateNodeState(DependencyGraphNodeStateDto node, ModuleStateInfo state)
    {
        // Update the state properties
        node.LatestActualState = state.LatestActualState;
        node.DesiredState = state.DesiredState;
        node.QueuedDesiredState = state.QueuedDesiredState;
        node.IsRunning = state.IsRunning;
        node.IsQueued = state.IsQueued;
        node.RunningDesiredState = state.RunningDesiredState;

        // Update derived display properties
        node.StateDisplayText = GetStateDisplayText(node.LatestActualState, node.IsRunning, node.IsQueued, node.RunningDesiredState, node.QueuedDesiredState);
        node.StateColor = GetStateColor(node.LatestActualState, node.IsRunning);
        node.NodeBorderStyle = GetNodeBorderStyle(node.LatestActualState, node.IsRunning);
        node.StateIcon = GetStateIcon(node.LatestActualState, node.IsRunning);
    }

    protected static string GetOrdinalSuffix(int number)
    {
        if (number <= 0) return number.ToString();

        return (number % 100) switch
        {
            11 or 12 or 13 => $"{number}th",
            _ => (number % 10) switch
            {
                1 => $"{number}st",
                2 => $"{number}nd",
                3 => $"{number}rd",
                _ => $"{number}th"
            }
        };
    }

    protected static string GetNamespaceDisplayName(string displayName)
    {
        // DisplayName format is "StackName/NamespaceName/ModuleName"
        var parts = displayName.Split('/');
        if (parts.Length >= 2) return $"{parts[0]}/{parts[1]}"; // Return "StackName/NamespaceName"
        return "Unknown Namespace";
    }

    protected static string GetModuleName(string displayName)
    {
        // DisplayName format is "StackName/NamespaceName/ModuleName"
        var parts = displayName.Split('/');
        if (parts.Length >= 3) return parts[2]; // Return just the module name
        return displayName; // Fallback to full display name if parsing fails
    }

    /// <summary>
    /// Creates a DependencyGraphNodeStateDto from a ModuleStateInfo
    /// </summary>
    /// <param name="moduleId">The module ID</param>
    /// <param name="moduleInfo">The module state info</param>
    /// <param name="targetState">The target state (Applied or Destroyed)</param>
    /// <returns>A new DependencyGraphNodeStateDto</returns>
    public static DependencyGraphNodeStateDto CreateNodeState(Guid moduleId, ModuleStateInfo moduleInfo, DesiredStateHeadline targetState)
    {
        return new DependencyGraphNodeStateDto
        {
            ModuleId = moduleId,
            NamespaceId = moduleInfo.NamespaceId,
            DisplayName = moduleInfo.DisplayName,
            ModuleName = moduleInfo.Name,
            NamespaceDisplayName = $"{moduleInfo.StackName}/{moduleInfo.NamespaceName}",
            LatestActualState = moduleInfo.LatestActualState,
            DesiredState = moduleInfo.DesiredState,
            QueuedDesiredState = moduleInfo.QueuedDesiredState,
            IsRunning = moduleInfo.IsRunning,
            IsQueued = moduleInfo.IsQueued,
            RunningDesiredState = moduleInfo.RunningDesiredState,
            TargetState = targetState,
            StateDisplayText = GetStateDisplayText(moduleInfo.LatestActualState, moduleInfo.IsRunning, moduleInfo.IsQueued, moduleInfo.RunningDesiredState, moduleInfo.QueuedDesiredState),
            StateColor = GetStateColor(moduleInfo.LatestActualState, moduleInfo.IsRunning),
            NodeBorderStyle = GetNodeBorderStyle(moduleInfo.LatestActualState, moduleInfo.IsRunning),
            StateIcon = GetStateIcon(moduleInfo.LatestActualState, moduleInfo.IsRunning)
        };
    }

    /// <summary>
    /// Builds a lookup dictionary of ModuleStateInfo from dependency edges
    /// </summary>
    public static Dictionary<Guid, ModuleStateInfo> BuildModuleInfoLookup(IEnumerable<Dependency> dependencies)
    {
        var lookup = new Dictionary<Guid, ModuleStateInfo>();

        foreach (var edge in dependencies)
        {
            if (!lookup.ContainsKey(edge.DefinedModuleId))
                lookup[edge.DefinedModuleId] = edge.ToDefinedModuleStateInfo();

            if (edge.ReferencedModuleId.HasValue && !lookup.ContainsKey(edge.ReferencedModuleId.Value))
            {
                var refInfo = edge.ToReferencedModuleStateInfo();
                if (refInfo != null)
                    lookup[edge.ReferencedModuleId.Value] = refInfo;
            }
        }

        return lookup;
    }

    /// <summary>
    /// Gets the CSS class for namespace cards based on their relationship to the primary namespace.
    ///
    /// Returns a class rather than an inline style because the treatment is theme-dependent: an
    /// external namespace is rendered *inverted* against the current surface (dark card in light
    /// mode, light card in dark mode), and a static method cannot know which theme is active.
    /// The rule lives with .dag-namespace-card in the graph flow components.
    /// </summary>
    /// <param name="namespaceId">The namespace ID to evaluate</param>
    /// <param name="primaryNamespaceId">The primary namespace ID (the one being operated on)</param>
    /// <param name="uninvolvedNamespaceIds">Set of namespace IDs that are uninvolved in the operation</param>
    /// <returns>CSS class for the namespace card, or empty for the primary namespace</returns>
    public static string GetNamespaceCardClass(Guid namespaceId, Guid? primaryNamespaceId, HashSet<Guid>? uninvolvedNamespaceIds = null)
    {
        var isExternalNamespace = primaryNamespaceId.HasValue && namespaceId != primaryNamespaceId.Value;

        // Applies regardless of involvement.
        return isExternalNamespace ? "dag-namespace-card-external" : "";
    }
}