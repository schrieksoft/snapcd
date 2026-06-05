// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.Modules;

/// <summary>
/// Source-repo coordinates for a Module: SourceType, SourceUrl, SourceRevision, SourceSubdirectory.
/// The actual file contents are not returned by SnapCd — clone the repo directly using these coordinates.
/// </summary>
public class ModuleSourceDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid NamespaceId { get; init; }
    public SourceType SourceType { get; init; }
    public string SourceUrl { get; init; } = string.Empty;
    public string SourceRevision { get; init; } = string.Empty;
    public SourceRevisionType SourceRevisionType { get; init; }
    public string SourceSubdirectory { get; init; } = string.Empty;
    public StateManagementEngine? Engine { get; init; }
}
