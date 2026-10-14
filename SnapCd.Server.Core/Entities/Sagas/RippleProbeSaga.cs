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

public class RippleProbeSaga : JobSagaBase
{
    /// <summary>The Module whose outputs changed, which is what prompted this probe.</summary>
    public Guid UpstreamModuleId { get; set; }

    /// <summary>The ModuleJob whose plan reported the change, when one did.</summary>
    public Guid? UpstreamModuleJobId { get; set; }

    /// <summary>
    /// The values to plan with, keyed by this Module's own variable names. Carried as encoded
    /// text the whole way: an unknown has no JSON representation, so the server never decodes it.
    /// </summary>
    public string InputsJson { get; set; } = null!;

    /// <summary>
    /// What the probe reported, held between the step's reply and the write that closes the job.
    /// Opaque to the server, as <see cref="InputsJson"/> is.
    /// </summary>
    public string? SignalsJson { get; set; }
}
