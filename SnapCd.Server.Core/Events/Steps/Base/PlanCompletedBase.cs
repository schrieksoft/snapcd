// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Events.Steps.Base;

public class PlanCompletedBase : StepResponseBase
{
    public int TotalCountAfter { get; set; }
    public int TotalCountBefore { get; set; }
    public int TotalChangedCount { get; set; }
    public int TotalUnchangedCount { get; set; }
    public int CreateCount { get; set; }
    public int ModifyCount { get; set; }
    public int DestroyCount { get; set; }
    public int RecreateCount { get; set; }

    public int OutputsTotalCount { get; set; }
    public int OutputsTotalChangedCount { get; set; }
    public int OutputsTotalUnchangedCount { get; set; }
    public int OutputsCreateCount { get; set; }
    public int OutputsModifyCount { get; set; }
    public int OutputsDestroyCount { get; set; }
    public int OutputsRecreateCount { get; set; }

    public string? OutputsUnchangedList { get; set; }
    public string? OutputsCreateList { get; set; }
    public string? OutputsModifyList { get; set; }
    public string? OutputsDestroyList { get; set; }
    public string? OutputsRecreateList { get; set; }
}