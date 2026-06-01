// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Newtonsoft.Json.Linq;
using SnapCd.Contracts;

namespace SnapCd.Runner.Services.Plan;

public class TerraformParsedPlan : IParsedPlan
{
    public required Tfplan.Plan Plan { get; set; }
    public required JObject State { get; set; }

    private static List<Tfplan.Action> MapToTfplanActions(PlanAction action)
    {
        return action switch
        {
            PlanAction.Noop => [Tfplan.Action.Noop],
            PlanAction.Create => [Tfplan.Action.Create],
            PlanAction.Update => [Tfplan.Action.Update],
            PlanAction.Delete => [Tfplan.Action.Delete],
            PlanAction.Replace => [Tfplan.Action.DeleteThenCreate, Tfplan.Action.CreateThenDelete],
            _ => []
        };
    }

    public int GetExistingCount()
    {
        if (State.TryGetValue("resources", out var resourcesToken) && resourcesToken is JArray resourcesArray)
            return resourcesArray.Count;

        return 0;
    }

    public int GetResourceCount(PlanAction action)
    {
        var tfActions = MapToTfplanActions(action);
        return Plan.ResourceChanges.Count(rc => tfActions.Contains(rc.Change.Action));
    }

    public int GetOutputCount(PlanAction action)
    {
        var tfActions = MapToTfplanActions(action);
        return Plan.OutputChanges.Count(rc => tfActions.Contains(rc.Change.Action));
    }

    public List<PlanResourceChange> GetResourceChange(PlanAction action)
    {
        var tfActions = MapToTfplanActions(action);
        return Plan.ResourceChanges
            .Where(rc => tfActions.Contains(rc.Change.Action))
            .Select(rc => new PlanResourceChange { Address = rc.Addr, Action = action })
            .ToList();
    }

    public List<PlanOutputChange> GetOutputChange(PlanAction action)
    {
        var tfActions = MapToTfplanActions(action);
        return Plan.OutputChanges
            .Where(rc => tfActions.Contains(rc.Change.Action))
            .Select(rc => new PlanOutputChange { Name = rc.Name, Action = action })
            .ToList();
    }
}
