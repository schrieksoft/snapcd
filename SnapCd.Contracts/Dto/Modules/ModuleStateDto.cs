// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.Modules;

/// <summary>
/// Module state-status summary: latest actual state, desired state, current execution status,
/// last job. Does NOT return the underlying state file (may contain secrets).
/// </summary>
public class ModuleStateDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid NamespaceId { get; init; }

    public Guid? LastJobId { get; init; }
    public string? LastJobType { get; init; }
    public string? LastActualStateHeadline { get; init; }
    public bool? LastIsCurrent { get; init; }
    public bool? LastWaitingForApproval { get; init; }
    public string? LastServerSideErrorHeader { get; init; }
}
