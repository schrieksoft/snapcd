// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;

namespace SnapCd.Runner.Services.Plan;

public interface IParsedPlan
{
    int GetExistingCount();
    int GetResourceCount(PlanAction action);
    int GetOutputCount(PlanAction action);
    List<PlanResourceChange> GetResourceChange(PlanAction action);
    List<PlanOutputChange> GetOutputChange(PlanAction action);
}

public class PlanResourceChange
{
    public string Address { get; set; } = "";
    public PlanAction Action { get; set; }
}

public class PlanOutputChange
{
    public string Name { get; set; } = "";
    public PlanAction Action { get; set; }

    /// <summary>
    /// The output's type, as the ctyjson bytes the plan held. Null for an engine whose plan does
    /// not carry one, which is not the same as an output with no type.
    /// </summary>
    public byte[]? EncodedType { get; set; }

    /// <summary>The prior value, still encoded. Absent when the output is being created.</summary>
    public byte[]? EncodedBefore { get; set; }

    /// <summary>
    /// The planned new value, still encoded. An unknown arrives here as msgpack fixext1 and must
    /// stay that way: it is the one value a downstream plan has to be told it cannot predict.
    /// </summary>
    public byte[]? EncodedAfter { get; set; }

    /// <summary>Whether the plan declared this output sensitive.</summary>
    public bool Sensitive { get; set; }
}
