// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Base;

namespace SnapCd.Server.Core.Events.Jobs.Module;

/// <summary>
/// Starts one Module's state move. A transfer is two of these, one per Module; nothing coordinates
/// them beyond the source's job being started after the receiver's has completed.
/// </summary>
public class TransferMigrateRequested : ModuleJobEventBase
{
    /// <summary>The Transfer both jobs belong to.</summary>
    public Guid TransferId { get; set; }

    /// <summary>Whether this Module gives the resources up or takes them on.</summary>
    public TransferRole Role { get; set; }

    /// <summary>This Module's root within its own checkout (--root-dir).</summary>
    public string? RootDirectory { get; set; }

    /// <summary>The ref this Module runs against.</summary>
    public string? ProveRef { get; set; }

    /// <summary>The fragment the source produced, for the receiver's job.</summary>
    public string? FragmentState { get; set; }

    public string? FragmentMeta { get; set; }

    /// <summary>The values the other Module produced that this one consumes, by filename.</summary>
    public Dictionary<string, string> Outputs { get; set; } = new();
}
