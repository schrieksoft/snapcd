// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;

namespace SnapCd.Server.Core.Dtos;

/// <summary>
/// A dependency graph scoped to one parent, which is a Stack or a Namespace. The view that draws it
/// needs the parent's identity, the direction, and the nodes; which kind of parent it is only
/// decides how the graph was built.
/// </summary>
public interface IDependencyGraphDto
{
    Guid ScopeId { get; }

    string ScopeName { get; }

    string Direction { get; set; }

    DesiredStateHeadline TargetState { get; set; }

    List<DependencyGraphNodeStateDto> NodeStates { get; set; }
}
