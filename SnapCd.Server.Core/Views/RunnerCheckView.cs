// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Views;

/// <summary>
/// View model for checking if the current principal can act as a runner for a specific Runner.
/// Used in runner checkin operations to validate permissions.
/// </summary>
public class RunnerCheckView
{
    /// <summary>
    /// The ID of the Runner.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the Runner.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Whether the current principal has the Runner role for this Runner.
    /// </summary>
    public bool CanActAsRunner { get; set; }
}