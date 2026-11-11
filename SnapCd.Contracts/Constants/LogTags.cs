// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Constants;

/// <summary>
/// The tags a job log line can carry. A tag says where a line came from, so a reader of the log
/// can treat one block differently from another without parsing the text to work out what it is.
///
/// These values are stored on every tagged line and are not rewritten, so a job logged under one
/// vocabulary keeps it. Add a value rather than renaming one.
/// </summary>
public static class LogTags
{
    /// <summary>What produced the line.</summary>
    public const string Producer = "producer";

    /// <summary>What the producer was doing, for one that does more than one thing.</summary>
    public const string Activity = "activity";

    /// <summary>The engine's own output, passed through as it printed it.</summary>
    public const string ProducerEngine = "engine";

    /// <summary>A hook the user supplied, run in the same shell as the engine.</summary>
    public const string ProducerHook = "hook";

    public const string ActivityInit = "init";
    public const string ActivityPlan = "plan";
    public const string ActivityApply = "apply";
    public const string ActivityDestroy = "destroy";
    public const string ActivityStateMigration = "state-migration";
    public const string ActivityOther = "other";

    /// <summary>The tags for a line the engine printed while doing one thing.</summary>
    public static Dictionary<string, string> Engine(string activity) => new()
    {
        [Producer] = ProducerEngine,
        [Activity] = activity
    };
}
