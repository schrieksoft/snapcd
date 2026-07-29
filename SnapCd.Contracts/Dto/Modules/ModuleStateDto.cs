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
    /// <summary>Unique ID of the Module.</summary>
    public Guid Id { get; init; }
    /// <summary>Name of the Module.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>ID of the Module's parent Namespace.</summary>
    public Guid NamespaceId { get; init; }

    /// <summary>ID of the Module's most recent Job.</summary>
    public Guid? LastJobId { get; init; }
    /// <summary>Type of the most recent Job: Apply or Destroy.</summary>
    public string? LastJobType { get; init; }
    /// <summary>Headline of the Module's actual state after the most recent Job.</summary>
    public string? LastActualStateHeadline { get; init; }
    /// <summary>True if the most recent Job ran against the Module's current definition and source revision.</summary>
    public bool? LastIsCurrent { get; init; }
    /// <summary>True if the most recent Job is waiting for an approval decision.</summary>
    public bool? LastWaitingForApproval { get; init; }
    /// <summary>Short server-side error header of the most recent Job, when it failed server-side.</summary>
    public string? LastServerSideErrorHeader { get; init; }
}
