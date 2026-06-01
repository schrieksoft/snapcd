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
}
