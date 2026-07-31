// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.NamespaceAdditionalTriggerPaths;

/// <summary>
/// DTO for creating a new NamespaceAdditionalTriggerPath (POST operations).
/// </summary>
public class NamespaceAdditionalTriggerPathCreateDto
{
    /// <summary>ID of the Namespace Additional Trigger Path's parent Namespace.</summary>
    public Guid NamespaceId { get; set; }

    /// <summary>Repo-root-relative directory that joins the trigger watch set of every Module in the Namespace that has path-scoped triggering enabled. Must be a normalized relative path that stays inside the repository (no leading slash, no `..` escaping the root). Must be unique in combination with `namespace_id`.</summary>
    public string Path { get; set; } = null!;
}
