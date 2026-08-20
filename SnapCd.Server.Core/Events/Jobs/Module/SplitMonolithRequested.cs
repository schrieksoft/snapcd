// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Server.Core.Events.Jobs.Base;

namespace SnapCd.Server.Core.Events.Jobs.Module;

public class SplitMonolithRequested : ModuleJobEventBase
{
    /// <summary>Monolith root within the checkout (--root-dir).</summary>
    public string? RootDirectory { get; set; }

    /// <summary>Replace a destination whose state does not match. Destructive: state push -force.</summary>
    public bool Force { get; set; }
}
