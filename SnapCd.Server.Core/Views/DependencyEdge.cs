// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Views;

/// <summary>
/// One Module depends on another, either by declaring it or by consuming one of its outputs.
///
/// Read-only: the table is maintained by triggers on DependsOnModules and ModuleInputs, and by
/// sp_RecomputeDependencyEdges. Nothing in the application writes to it.
/// </summary>
public class DependencyEdge
{
    public Guid DefinedModuleId { get; set; }
    public Guid ReferencedModuleId { get; set; }
    public Guid OrganizationId { get; set; }
}
