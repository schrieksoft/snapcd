// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;

namespace SnapCd.Server.Core.Dtos;

/// <summary>Notification payload marking a remote source as changed (POST operations).</summary>
public class SourceChangedDto
{
    /// <summary>Remote URL of the source that changed.</summary>
    public string SourceUrl { get; set; } = null!;
    /// <summary>Revision (branch, tag or commit) that changed.</summary>
    public string SourceRevision { get; set; } = null!;

    public SourceType SourceType { get; set; } = SourceType.Git;
}