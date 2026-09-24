// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Events.Jobs.Base;

namespace SnapCd.Server.Core.Events.Jobs.Module;

/// <summary>
/// Starts one Module's state move. Each side is started on its own; nothing coordinates them.
/// </summary>
public class TransferMigrateRequested : ModuleJobEventBase
{
    /// <summary>The Module on the other side of the move.</summary>
    public Guid CounterpartyModuleId { get; set; }

    /// <summary>This Module's root within its own checkout (--root-dir).</summary>
    public string? RootDirectory { get; set; }

    /// <summary>The ref this Module runs against.</summary>
    public string? ProveRef { get; set; }

}
