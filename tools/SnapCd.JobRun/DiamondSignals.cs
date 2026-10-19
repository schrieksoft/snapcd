// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.JobRun;

/// <summary>
/// What a real planwalker reported for the diamond fixture's roots, captured once and replayed here.
///
/// a changes an output; b and c each read it; d reads both. The shape matters because d's two
/// contributions resolve at different moments, so it is the only one that exercises waiting for a
/// second upstream and merging two unknowns into one plan.
///
/// a and b each publish a second output that does not change, read by two further Modules that a
/// wave must therefore leave alone. Those appear here as action "no-op" with an unchanged value,
/// which is how planwalker reports an output it considered and found unaffected.
///
/// These bytes came from OpenTofu planning real state, not from a hand-written envelope. A changed
/// output carries after "1AAA", the msgpack fixext1 for an unknown value, because the resource
/// behind it is replaced and its id cannot be known until it applies. A hand-written stand-in
/// would be a guess at a format only planwalker produces.
/// </summary>
public static class DiamondSignals
{
    /// <summary>
    /// The changed output that starts a wave. The pet is replaced because its keepers changed, so
    /// its id is unknown rather than merely different.
    /// </summary>
    public const string RootA = """
        {
          "format_version": "1",
          "outputs": {
            "a_id": {
              "action": "unknown",
              "type": "InN0cmluZyI=",
              "before": "rHRydXN0aW5nLWxhYg==",
              "after": "1AAA"
            },
            "a_other": {
              "action": "no-op",
              "type": "InN0cmluZyI=",
              "before": "rmhvbmVzdC1tZWVya2F0",
              "after": "rmhvbmVzdC1tZWVya2F0"
            }
          }
        }
        """;

    /// <summary>
    /// What b reports once a's unknown reaches it. Its own pet is replaced in turn, which is the
    /// cascade plain OpenTofu cannot see: planned against a's committed value it reports no
    /// changes at all.
    /// </summary>
    public const string RootB = """
        {
          "format_version": "1",
          "outputs": {
            "b_id": {
              "action": "unknown",
              "type": "InN0cmluZyI=",
              "before": "uXRydXN0aW5nLWxhYi13aWxsaW5nLWdydWI=",
              "after": "1AAA"
            },
            "b_other": {
              "action": "no-op",
              "type": "InN0cmluZyI=",
              "before": "rGNhdXNhbC1idXJybw==",
              "after": "rGNhdXNhbC1idXJybw=="
            }
          }
        }
        """;

    public const string RootC = """
        {
          "format_version": "1",
          "outputs": {
            "c_id": {
              "action": "unknown",
              "type": "InN0cmluZyI=",
              "before": "uHRydXN0aW5nLWxhYi1maW5lLWdpYmJvbg==",
              "after": "1AAA"
            }
          }
        }
        """;

    /// <summary>
    /// The far end of the diamond, reached only after both arms report. Its plan merges two
    /// unknowns into one expression, which is the case a chain of single dependencies never
    /// reaches.
    /// </summary>
    public const string RootD = """
        {
          "format_version": "1",
          "outputs": {
            "d_id": {
              "action": "unknown",
              "type": "InN0cmluZyI=",
              "before": "2UR0cnVzdGluZy1sYWItd2lsbGluZy1ncnViLXRydXN0aW5nLWxhYi1maW5lLWdpYmJvbi1kaXNjcmV0ZS1hbnRlYXRlcg==",
              "after": "1AAA"
            }
          }
        }
        """;
}
