// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// Values for <see cref="ManualModuleJob.JobType"/>. The value names the saga table holding the
/// job's state, so with the job's Id it is a complete pointer to that state.
/// </summary>
public static class ManualJobTypes
{
    public const string SplitMigrate = nameof(Sagas.SplitMigrateSaga);

    /// <summary>The pre-merge half of a split: the same saga, stopping after the proof. State lives in SplitMigrateSagas too.</summary>
    public const string SplitProve = "SplitProve";

    /// <summary>The pre-merge half of a transfer: proves both participants, holds nothing, writes nothing.</summary>
    public const string TransferProve = "TransferProve";

    /// <summary>The post-merge half of a transfer: the one that pushes state, under holds and consent.</summary>
    public const string TransferMigrate = "TransferMigrate";
}
