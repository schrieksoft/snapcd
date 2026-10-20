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
            .Select(rc => ToOutputChange(rc, action))
            .ToList();
    }

    /// <summary>
    /// One output change with its encoded values kept. Change.Values holds the before and after
    /// in that order, each a msgpack [type-as-ctyjson, value] pair, and a creation has no before.
    /// </summary>
    private static PlanOutputChange ToOutputChange(Tfplan.OutputChange change, PlanAction action)
    {
        var result = new PlanOutputChange
        {
            Name = change.Name,
            Action = action,
            Sensitive = change.Sensitive
        };

        // Which values a change carries follows its action: a creation has only the new value, a
        // deletion only the old one, and anything else carries both in that order. Reading by
        // position alone puts a creation's new value in the before slot.
        var values = change.Change.Values;

        var (before, after) = change.Change.Action switch
        {
            Tfplan.Action.Create when values.Count > 0 =>
                (null, PlanOutputSignalReader.Split(values[0].Msgpack.Span)),
            Tfplan.Action.Delete when values.Count > 0 =>
                (PlanOutputSignalReader.Split(values[0].Msgpack.Span), null),
            _ => (
                values.Count > 0 ? PlanOutputSignalReader.Split(values[0].Msgpack.Span) : null,
                values.Count > 1 ? PlanOutputSignalReader.Split(values[1].Msgpack.Span) : null)
        };

        result.EncodedType = after?.Type ?? before?.Type;
        result.EncodedBefore = before?.Value;
        result.EncodedAfter = after?.Value;

        return result;
    }
}
