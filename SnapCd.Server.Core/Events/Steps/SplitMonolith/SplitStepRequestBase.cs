// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps.SplitMonolith;

/// <summary>
/// Parameters every demonolith step needs. Declared once here rather than repeated on each
/// request, so the send activity can set them against a compile-checked type.
/// </summary>
public abstract class SplitStepRequestBase : StepRequestBase
{
    /// <summary>Monolith root within the checkout (--root-dir).</summary>
    public string? RootDirectory { get; set; }
}
