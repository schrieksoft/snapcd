// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.RunnerRequests;

/// <summary>
/// Result payload for a path-aware source refresh (SourceRefreshCompletedV2). Carries the resolved head revision
/// plus the git tree hash of every watched directory at that revision.
/// </summary>
public class SourceRefreshResult
{
    public required string DefinitiveRevision { get; set; }

    public List<PathHash> PathHashes { get; set; } = new();

    /// <summary>
    /// Per watched root, the transitive closure of locally-referenced terraform directories discovered at the
    /// refreshed revision. Null when discovery did not run (tool unavailable or failed) — the server then
    /// composes over declared paths only.
    /// </summary>
    public List<ModuleClosure>? ModuleClosures { get; set; }

    /// <summary>Echo of SourceRefreshRequest.TriggeredByNotification.</summary>
    public bool TriggeredByNotification { get; set; }
}

/// <summary>
/// The locally-referenced directories reachable from one watched root at the refreshed revision, as resolved by
/// snapcd-inspect from literal local module sources. Paths inside the root's own subtree are not repeated here.
/// </summary>
public class ModuleClosure
{
    public required string RootPath { get; set; }

    public List<string> ReferencedPaths { get; set; } = new();
}

/// <summary>
/// Git tree object hash of one watched directory at the refreshed revision. A directory that does not exist at
/// that revision reports an empty TreeHash, which the server treats as "changed if it previously existed".
/// </summary>
public class PathHash
{
    public required string Path { get; set; }

    public required string TreeHash { get; set; }
}
