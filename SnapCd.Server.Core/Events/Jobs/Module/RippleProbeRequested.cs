// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using SnapCd.Server.Core.Events.Jobs.Base;

namespace SnapCd.Server.Core.Events.Jobs.Module;

public class RippleProbeRequested : ModuleJobEventBase
{
    /// <summary>The Module whose outputs changed.</summary>
    public Guid UpstreamModuleId { get; set; }

    /// <summary>The ModuleJob whose plan reported the change, when one did.</summary>
    public Guid? UpstreamModuleJobId { get; set; }

    /// <summary>
    /// The values to plan with, keyed by this Module's own variable names. Carried as encoded
    /// text the whole way: an unknown has no JSON representation, so the server never decodes it.
    /// </summary>
    public string InputsJson { get; set; } = null!;
}
