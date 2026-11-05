// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Views;

/// <summary>
/// One reachable pair in the dependency closure: Root depends on Defined, which depends on
/// Referenced, Depth hops down. Direction 1 is the apply order, 2 the destroy order.
///
/// The names are denormalised onto the row, so reading the closure needs no join to Modules,
/// Namespaces or Stacks. Read-only: sp_RecomputeRecursiveDependencyEdges and the triggers on
/// DependencyEdges maintain it.
/// </summary>
public class RecursiveDependencyEdge
{
    public Guid RootModuleId { get; set; }
    public Guid RootOrganizationId { get; set; }
    public string RootModuleName { get; set; } = null!;
    public Guid RootNamespaceId { get; set; }
    public string RootNamespaceName { get; set; } = null!;
    public Guid RootStackId { get; set; }
    public string RootStackName { get; set; } = null!;
    public string RootDisplayName { get; set; } = null!;

    public Guid DefinedModuleId { get; set; }
    public Guid DefinedOrganizationId { get; set; }
    public string DefinedModuleName { get; set; } = null!;
    public Guid DefinedNamespaceId { get; set; }
    public string DefinedNamespaceName { get; set; } = null!;
    public Guid DefinedStackId { get; set; }
    public string DefinedStackName { get; set; } = null!;
    public string DefinedDisplayName { get; set; } = null!;

    public Guid ReferencedModuleId { get; set; }
    public Guid ReferencedOrganizationId { get; set; }
    public string ReferencedModuleName { get; set; } = null!;
    public Guid ReferencedNamespaceId { get; set; }
    public string ReferencedNamespaceName { get; set; } = null!;
    public Guid ReferencedStackId { get; set; }
    public string ReferencedStackName { get; set; } = null!;
    public string ReferencedDisplayName { get; set; } = null!;

    public int Depth { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>1 for apply, 2 for destroy. The two recursive views differ only by this.</summary>
    public byte Direction { get; set; }
}
