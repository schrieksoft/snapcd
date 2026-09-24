// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Sagas;

/// <summary>
/// Moves, imports or removes addresses in a Module's state, then asks the state what is actually
/// there. The move says which addresses it managed; the list that follows says which of them are
/// where they were meant to be, and it is the list that anything watching acts on.
/// </summary>
public class StateMoveSaga : ManualJobSagaBase
{
    public StateEditOperation Operation { get; set; }

    /// <summary>The addresses and their targets, as JSON.</summary>
    public string InstructionsJson { get; set; } = null!;

    /// <summary>The addresses the move reported managing, as JSON: what the list then asks about.</summary>
    [MaxLength(4000)] public string? SucceededJson { get; set; }

    /// <summary>How many addresses the move could not manage, which is what makes a job partial.</summary>
    public int FailedCount { get; set; }
}
