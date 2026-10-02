// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Events.System;

/// <summary>
/// Carries nothing and means nothing: it exists so a message can be put through the transport
/// without reaching anything that acts on it. A message sent on a cold SQL Server transport is
/// sometimes never delivered, and sending one of these first is what makes the next one arrive.
/// </summary>
public class WarmupRequested
{
    /// <summary>Which of a burst this was, so the log shows how many got through.</summary>
    public int Sequence { get; set; }
}
