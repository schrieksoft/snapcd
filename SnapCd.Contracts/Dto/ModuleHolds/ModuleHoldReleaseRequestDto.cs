// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.ModuleHolds;

/// <summary>Releases the listed holds at once; holds belonging to another principal are left alone.</summary>
public class ModuleHoldReleaseRequestDto
{
    /// <summary>The Modules to release.</summary>
    public List<Guid> ModuleIds { get; set; } = [];

    /// <summary>The Namespaces to release.</summary>
    public List<Guid> NamespaceIds { get; set; } = [];

    /// <summary>The Stacks to release.</summary>
    public List<Guid> StackIds { get; set; } = [];
}
