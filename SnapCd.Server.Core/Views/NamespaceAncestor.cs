// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
namespace SnapCd.Server.Core.Views;

/// <summary>
/// One row per Namespace and each Namespace above it, itself included at depth 0. Read-only: the
/// table is maintained by triggers, so nothing in the application writes to it.
/// </summary>
public class NamespaceAncestor
{
    /// <summary>The Namespace the row is about.</summary>
    public Guid NamespaceId { get; set; }

    /// <summary>A Namespace at or above <see cref="NamespaceId"/>.</summary>
    public Guid AncestorNamespaceId { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>Hops from the Namespace to the ancestor. Zero where they are the same.</summary>
    public int Depth { get; set; }
}
