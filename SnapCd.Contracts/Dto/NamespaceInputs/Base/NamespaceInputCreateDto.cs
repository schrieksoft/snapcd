// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.NamespaceInputs.Base;

public class NamespaceInputCreateDto
{
    /// <summary>ID of the Namespace Input's parent Namespace.</summary>
    public Guid NamespaceId { get; set; }
    /// <summary>Name of the Namespace Input. Must be unique in combination with `namespaceId`.</summary>
    public string Name { get; set; } = null!;

    /// <summary>No longer has any effect: every Namespace Input reaches every Module in the Namespace and below it, whatever this is set to. Accepted and stored so that existing clients keep working. To keep an input from a Module or a Namespace, declare an Unset input of the same name there.</summary>
    public NamespaceInputUsageMode UsageMode { get; set; }

    /// <summary>The kind of input.</summary>
    public InputKind InputKind { get; set; }
}
