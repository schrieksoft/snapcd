// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Services.Nonsecured.DependencyGraph;
using SnapCd.Server.Core.Services.Utils.Notification;

namespace SnapCd.Server.Core.UI.Dashboard.Components.DependencyGraph;

/// <summary>
/// Everything a dependency graph view does that does not depend on what the graph is scoped to:
/// loading it, caching the apply and the destroy direction, following the modules in it, and
/// sizing the scroll area. A level supplies only how its graph is built.
/// </summary>
public abstract class DependencyGraphViewBase<TGraph> : ComponentBase, IDisposable
    where TGraph : class, IDependencyGraphDto
{
    [Inject] protected ApplyModuleGraphServiceFactory ApplyServiceFactory { get; set; } = null!;
    [Inject] protected DestroyModuleGraphServiceFactory DestroyServiceFactory { get; set; } = null!;
    [Inject] protected ModuleStateModifiedNotificationService ModuleStateModifiedNotificationService { get; set; } = null!;
    [Inject] protected DependencyGraphServiceFactory DependencyGraphServiceFactory { get; set; } = null!;
    [Inject] protected IJSRuntime JS { get; set; } = null!;

    [Parameter] public string Direction { get; set; } = "Apply";

    [Parameter] public bool DisableDirectionToggle { get; set; }

    [CascadingParameter] public Guid OrganizationId { get; set; }

    protected bool IsLoading = true;
    protected string? Error;
    protected TGraph? Graph;
    protected bool ShowDestroyGraph;
    protected ElementReference ScrollContainer;

    private TGraph? _applyGraph;
    private TGraph? _destroyGraph;
    private readonly HashSet<Guid> _subscribedModuleIds = [];
    private string _previousDirection = string.Empty;
    private Guid _previousScopeId;

    /// <summary>The scope this view draws, so the base can tell when the caller points it elsewhere.</summary>
    protected abstract Guid ScopeId { get; }

    protected abstract Task<TGraph> BuildApplyGraph();

    protected abstract Task<TGraph> BuildDestroyGraph();

    protected override async Task OnInitializedAsync()
    {
        _previousScopeId = ScopeId;
        _previousDirection = Direction;
        ShowDestroyGraph = Direction == "Destroy";

        await LoadGraph();
        UpdateSubscriptions();
    }

    protected override async Task OnParametersSetAsync()
    {
        ShowDestroyGraph = Direction == "Destroy";

        if (ScopeId == _previousScopeId && Direction == _previousDirection)
        {
            StateHasChanged();
            return;
        }

        _previousScopeId = ScopeId;
        _previousDirection = Direction;

        _applyGraph = null;
        _destroyGraph = null;

        await LoadGraph();
        UpdateSubscriptions();
    }

    private async Task LoadGraph()
    {
        IsLoading = true;
        Error = null;

        try
        {
            if (ShowDestroyGraph)
            {
                _destroyGraph = await BuildDestroyGraph();
                Graph = _destroyGraph;
            }
            else
            {
                _applyGraph = await BuildApplyGraph();
                Graph = _applyGraph;
            }
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Keeps the already-built direction, so toggling back does not rebuild it.</summary>
    protected async Task OnShowDestroyGraphChanged(bool value)
    {
        ShowDestroyGraph = value;

        var cached = value ? _destroyGraph : _applyGraph;

        if (cached != null)
            Graph = cached;
        else
            await LoadGraph();

        UpdateSubscriptions();
    }

    protected string GetToggleTooltipText()
    {
        if (DisableDirectionToggle)
            return "Toggle is disabled while \"Apply All Modules\" or \"Destroy All Modules\" operation is active";

        return ShowDestroyGraph
            ? "Switch to Apply dependency graph view"
            : "Switch to Destroy dependency graph view";
    }

    private void UpdateSubscriptions()
    {
        var wanted = new HashSet<Guid>();

        foreach (var graph in new[] { _applyGraph, _destroyGraph })
        {
            if (graph == null) continue;

            foreach (var node in graph.NodeStates) wanted.Add(node.ModuleId);
        }

        foreach (var moduleId in _subscribedModuleIds.Except(wanted).ToList())
        {
            ModuleStateModifiedNotificationService.Unsubscribe(moduleId, HandleModuleStateChanged);
            _subscribedModuleIds.Remove(moduleId);
        }

        foreach (var moduleId in wanted.Except(_subscribedModuleIds).ToList())
        {
            ModuleStateModifiedNotificationService.Subscribe(moduleId, HandleModuleStateChanged);
            _subscribedModuleIds.Add(moduleId);
        }
    }

    private async Task HandleModuleStateChanged(Guid moduleId)
    {
        using var dependencyGraphService = DependencyGraphServiceFactory.Create();
        var moduleState = await dependencyGraphService.GetModuleStateAsync(moduleId);

        if (moduleState == null) return;

        var updated = false;

        foreach (var graph in new[] { _applyGraph, _destroyGraph })
        {
            var node = graph?.NodeStates.FirstOrDefault(n => n.ModuleId == moduleId);

            if (node == null) continue;

            ModuleGraphServiceBase.UpdateNodeState(node, moduleState);
            updated = true;
        }

        if (updated) await InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        foreach (var moduleId in _subscribedModuleIds)
            ModuleStateModifiedNotificationService.Unsubscribe(moduleId, HandleModuleStateChanged);

        _subscribedModuleIds.Clear();
    }
}
