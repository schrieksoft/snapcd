// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.JobRun;

/// <summary>
/// What each root of the fanout fixture reports, replayed here.
///
/// a changes an output and b, c and d all read it. Unlike the diamond, which exercises how far a
/// wave travels, this one exercises what the panel says about each participant once it arrives:
/// every consumer answers, and the three answers are the three ways a probe can report.
///
/// b changes resources and no outputs. c changes nothing and says so by carrying its outputs
/// unchanged. d answers with nothing at all.
/// </summary>
public static class FanoutSignals
{
    /// <summary>
    /// The changed output that starts the wave. Unknown after, because the resource behind it is
    /// replaced and its id cannot be known until it applies.
    /// </summary>
    public const string RootA = """
        {
          "format_version": "1",
          "outputs": {
            "a_id": {
              "action": [ "update" ],
              "type": "string",
              "before": "settled-marmot",
              "after": {
                "$unknown": true
              }
            }
          },
          "resource_changes": [
            {
              "address": "random_pet.a",
              "type": "random_pet",
              "name": "a",
              "action": [ "delete", "create" ]
            }
          ]
        }
        """;

    /// <summary>
    /// b updates two resources in place and passes nothing on: its own output is computed from
    /// values that did not move, so it resolves to what it already was.
    ///
    /// This is the case that reads as "no change" on a panel counting only signals, while the
    /// module's own log shows two updates. Its resource changes are the whole finding.
    /// </summary>
    public const string RootB = """
        {
          "format_version": "1",
          "outputs": {
            "b_id": {
              "action": [ "no-op" ],
              "type": "string",
              "before": "settled-marmot-plain-asp",
              "after": "settled-marmot-plain-asp"
            }
          },
          "resource_changes": [
            {
              "address": "null_resource.b_first",
              "type": "null_resource",
              "name": "b_first",
              "action": [ "update" ]
            },
            {
              "address": "module.inner.null_resource.b_second",
              "type": "null_resource",
              "name": "b_second",
              "action": [ "update" ]
            }
          ]
        }
        """;

    /// <summary>
    /// c is genuinely unaffected. Every output is carried, because a consumer needs a value for
    /// each variable it reads, so an unaffected Module still reports signals: three of them, all
    /// no-ops, and no resource changes.
    ///
    /// This is the case that reads as "changes" on a panel counting signal rows.
    /// </summary>
    public const string RootC = """
        {
          "format_version": "1",
          "outputs": {
            "c_ip": {
              "action": [ "no-op" ],
              "type": "string",
              "before": "100.0.8.255",
              "after": "100.0.8.255"
            },
            "c_fqdn": {
              "action": [ "no-op" ],
              "type": "string",
              "before": "fanout-c.example.com",
              "after": "fanout-c.example.com"
            },
            "c_id": {
              "action": [ "no-op" ],
              "type": "string",
              "before": "settled-marmot-sunny-crab",
              "after": "settled-marmot-sunny-crab"
            }
          },
          "resource_changes": []
        }
        """;

    /// <summary>
    /// d answers with nothing. An empty reply used to leave the attempt unrecorded, so the Module
    /// read as still running on a job that had already completed and the wave never settled.
    /// </summary>
    public const string RootD = "";
}
