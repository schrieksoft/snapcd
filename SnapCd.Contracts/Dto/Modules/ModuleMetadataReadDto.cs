// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Modules;

/// <summary>Name and id of a Module, for callers who may discover it without reading it.</summary>
public class ModuleMetadataReadDto : IDto
{
    /// <summary>Unique ID of the Module.</summary>
    public Guid Id { get; set; }

    /// <summary>Name of the Module.</summary>
    public string Name { get; set; } = null!;

    /// <summary>ID of the Namespace this Module belongs to.</summary>
    public Guid NamespaceId { get; set; }
}
