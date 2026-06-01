// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

/// <summary>
/// Data returned when a Plan operation completes successfully.
/// Contains all resource and output change statistics.
/// </summary>
public class PlanCompletedData
{
    // Resource counts
    public int TotalCountAfter { get; set; }
    public int TotalCountBefore { get; set; }
    public int TotalChangedCount { get; set; }
    public int TotalUnchangedCount { get; set; }
    public int CreateCount { get; set; }
    public int ModifyCount { get; set; }
    public int DestroyCount { get; set; }
    public int RecreateCount { get; set; }

    // Output counts
    public int OutputsTotalCount { get; set; }
    public int OutputsTotalChangedCount { get; set; }
    public int OutputsTotalUnchangedCount { get; set; }
    public int OutputsCreateCount { get; set; }
    public int OutputsModifyCount { get; set; }
    public int OutputsDestroyCount { get; set; }
    public int OutputsRecreateCount { get; set; }

    // Output name lists
    public List<string>? OutputsUnchangedList { get; set; }
    public List<string>? OutputsCreateList { get; set; }
    public List<string>? OutputsModifyList { get; set; }
    public List<string>? OutputsDestroyList { get; set; }
    public List<string>? OutputsRecreateList { get; set; }
}
