// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.UI.Dashboard.Components;

/// <summary>
/// A request from the tree to create a child at one of its levels, raised by the plus beside that
/// level's heading. A Stack has no parent, so its id is empty.
/// </summary>
/// <param name="Scope">The level being created at.</param>
/// <param name="ParentId">The Stack a Namespace goes in, or the Namespace a Module goes in.</param>
public record HierarchyCreation(SearchScope Scope, Guid ParentId = default)
{
    public static HierarchyCreation OfStack() => new(SearchScope.Stack);

    public static HierarchyCreation OfNamespace(Guid stackId) => new(SearchScope.Namespace, stackId);

    public static HierarchyCreation OfModule(Guid namespaceId) => new(SearchScope.Module, namespaceId);
}
