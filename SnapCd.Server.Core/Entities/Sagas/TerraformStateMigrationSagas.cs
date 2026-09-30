// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using SnapCd.Server.Core.Entities.Sagas.Base;

namespace SnapCd.Server.Core.Entities.Sagas;

/// <summary>
/// What an edit to a Module's state is doing. The edit says which addresses it managed; the list
/// that follows says which of them are where they were meant to be, and it is the list that
/// anything watching acts on.
/// </summary>
public abstract class TerraformStateMigrationSagaBase : StateMigrationSagaBase
{
    /// <summary>The addresses and their targets, as JSON.</summary>
    public string InstructionsJson { get; set; } = null!;

    /// <summary>The addresses the edit reported managing, as JSON: what the list then asks about.</summary>
    [MaxLength(4000)] public string? SucceededJson { get; set; }

    /// <summary>How many addresses the edit could not manage, which is what makes a job partial.</summary>
    public int FailedCount { get; set; }
}

/// <summary>Moving addresses to where they should be, after a dry run says what would move.</summary>
public class MoveSaga : TerraformStateMigrationSagaBase;

/// <summary>Importing addresses from ids they already have, after checking the addresses are free.</summary>
public class ImportSaga : TerraformStateMigrationSagaBase;

/// <summary>Taking addresses out of state, after a dry run says what would go.</summary>
public class RemoveSaga : TerraformStateMigrationSagaBase;
