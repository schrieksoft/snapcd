// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Contracts.Dto.Modules;

/// <summary>
/// Whether a Module is held out of the automated lifecycle, and by whom. A paused Module dispatches
/// no triggered or dependency-driven work; requests raised while it is paused are queued rather than lost.
/// </summary>
public class ModulePauseDto
{
    /// <summary>Unique ID of the Module.</summary>
    public Guid ModuleId { get; init; }
    /// <summary>True while the Module is held out of the automated lifecycle.</summary>
    public bool Paused { get; init; }
    /// <summary>ID of the principal that paused the Module, when paused.</summary>
    public Guid? PausedBy { get; init; }
    /// <summary>When the Module was paused, when paused.</summary>
    public DateTime? PausedAt { get; init; }
    /// <summary>Operator-supplied explanation for the pause, when one was given.</summary>
    public string? PauseReason { get; init; }
}
