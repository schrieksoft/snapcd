// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Runner.Settings;

/// <summary>
/// Tuning for the bare-clone cache the Runner keeps for path-aware source refreshes. Clones live under
/// &lt;WorkingDirectory&gt;/sourcecache and are metadata-only (blob-filtered) where the git host supports it.
/// </summary>
public class SourceCacheSettings
{
    /// <summary>
    /// Maximum total size of the bare-clone cache in megabytes. When exceeded, least-recently-used clones are
    /// evicted (and re-cloned on next use). 0 (default) disables eviction.
    /// </summary>
    public int MaxSizeMb { get; set; } = 0;

    /// <summary>
    /// When true, clones are blob-filtered (--filter=blob:none): smaller on disk and faster to create for
    /// repositories carrying large binary artifacts, at the cost of referenced-folder discovery lazily fetching
    /// each previously-unseen terraform file over the network one round trip at a time whenever a commit changes
    /// it. False (default) uses full bare clones: new blobs arrive batched in the regular fetch, keeping
    /// discovery entirely local — the right trade for typical text-only terraform repositories.
    /// </summary>
    public bool BlobFilterEnabled { get; set; } = false;

    /// <summary>
    /// Executable used for referenced-folder discovery during path-aware refreshes. By default the Runner uses
    /// the snapcd-inspect binary embedded in its own build (extracted to &lt;WorkingDirectory&gt;/tools on first
    /// use), falling back to "snapcd-inspect" on PATH when no binary is embedded. Set an explicit path to
    /// override both; when no binary is available at all, refreshes proceed without discovery (declared paths
    /// only).
    /// </summary>
    public string InspectBinaryPath { get; set; } = "snapcd-inspect";
}
