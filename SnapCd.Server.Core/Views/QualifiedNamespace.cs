// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Views;

/// <summary>
/// A Namespace with the Stack above it, for a picker where the name alone does not say which
/// one it is. Metadata carries the StackId but not its name, and is kept join-free.
/// </summary>
public class QualifiedNamespace
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string Name { get; set; } = null!;

    public string StackName { get; set; } = null!;

    /// <summary>The name with the Stack above it, as a picker shows it.</summary>
    public string QualifiedName => $"{StackName} / {Name}";
}
