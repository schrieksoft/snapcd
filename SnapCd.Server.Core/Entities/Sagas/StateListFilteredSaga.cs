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
/// Asks which addresses are in a Module's state. It runs the same preamble as any other job
/// because listing state needs a checkout and an initialised backend, then reports and ends.
/// </summary>
public class StateListFilteredSaga : ManualJobSagaBase
{
    /// <summary>The addresses to ask about, as JSON. Nothing is reported about any other.</summary>
    [MaxLength(4000)] public string AddressesJson { get; set; } = null!;
}
