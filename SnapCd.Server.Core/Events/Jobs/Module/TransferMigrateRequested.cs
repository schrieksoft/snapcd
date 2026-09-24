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
/// Starts one Module's state move. A transfer run is one of these per Module in its scope, each
/// running independently; the ledger records what actually moved.
/// </summary>
public class TransferMigrateRequested : ModuleJobEventBase
{
    /// <summary>The Transfer this run belongs to.</summary>
    public Guid TransferId { get; set; }

    /// <summary>The run that started this job.</summary>
    public Guid TransferRunId { get; set; }

    /// <summary>This Module's root within its own checkout (--root-dir).</summary>
    public string? RootDirectory { get; set; }

    /// <summary>The ref this Module runs against.</summary>
    public string? ProveRef { get; set; }

}
