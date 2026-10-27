// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Runner.Logging;

/// <summary>
/// The log streams, one per table that stores runner logs. A caller names the one it means, the
/// way a shared runner task is handed the callbacks its replies go to.
/// </summary>
public interface IJobLogStreams
{
    /// <summary>Deployment jobs: Apply and Destroy both log here.</summary>
    IJobLogStream ModuleJobs { get; }

    /// <summary>Manual jobs: Move, Import, Remove and SplitMigrate all log here.</summary>
    IJobLogStream StateMigrations { get; }
}
