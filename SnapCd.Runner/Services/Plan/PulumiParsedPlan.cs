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

public class PulumiParsedPlan : IParsedPlan
{
    private readonly JObject _json;

    public PulumiParsedPlan(JObject json)
    {
        _json = json;
    }

    private static PlanAction MapOp(string op)
    {
        return op switch
        {
            "same" => PlanAction.Noop,
            "create" => PlanAction.Create,
            "update" => PlanAction.Update,
            "delete" => PlanAction.Delete,
            "replace" or "create-replacement" or "delete-replaced" => PlanAction.Replace,
            _ => PlanAction.Noop
        };
    }

    private List<(string Urn, PlanAction Action)> GetResourceOps()
    {
        // Plan file format: resourcePlans map with steps arrays of strings
        if (_json["resourcePlans"] is JObject resourcePlans)
        {
            var results = new List<(string, PlanAction)>();
            foreach (var prop in resourcePlans.Properties())
            {
                var urn = prop.Name;
                var type = prop.Value["goal"]?["type"]?.ToString() ?? "";
                if (type == "pulumi:pulumi:Stack") continue;

                if (prop.Value["steps"] is not JArray steps) continue;

                foreach (var step in steps)
                {
                    results.Add((urn, MapOp(step.ToString())));
                }
            }

            return results;
        }

        // Preview JSON format: top-level steps array with op/urn objects
        if (_json["steps"] is JArray previewSteps)
        {
            return previewSteps
                .Where(s => (s["newState"]?["type"]?.ToString() ?? s["type"]?.ToString() ?? "") != "pulumi:pulumi:Stack")
                .Select(s => (
                    s["urn"]?.ToString() ?? "",
                    MapOp(s["op"]?.ToString() ?? "")
                ))
                .ToList();
        }

        return [];
    }

    public int GetExistingCount()
    {
        if (_json["changeSummary"] is JObject summary)
        {
            var same = summary["same"]?.Value<int>() ?? 0;
            var update = summary["update"]?.Value<int>() ?? 0;
            var delete = summary["delete"]?.Value<int>() ?? 0;
            var replace = summary["replace"]?.Value<int>() ?? 0;
            return same + update + delete + replace;
        }

        return GetResourceOps().Count(r => r.Action != PlanAction.Create);
    }

    public int GetResourceCount(PlanAction action)
    {
        return GetResourceOps().Count(r => r.Action == action);
    }

    public int GetOutputCount(PlanAction action)
    {
        return 0;
    }

    public List<PlanResourceChange> GetResourceChange(PlanAction action)
    {
        return GetResourceOps()
            .Where(r => r.Action == action)
            .Select(r => new PlanResourceChange
            {
                Address = r.Urn,
                Action = action
            })
            .ToList();
    }

    public List<PlanOutputChange> GetOutputChange(PlanAction action)
    {
        return [];
    }
}
