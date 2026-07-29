// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.Runners;

/// <summary>
/// DTO for creating a new Runner (POST operations).
/// </summary>
public class RunnerCreateDto
{
    /// <summary>ID of the Service Principal associated with the Runner.</summary>
    public Guid ServicePrincipalId { get; set; }

    /// <summary>Unique name of the Runner.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Indicates whether or not the Runner is disabled</summary>
    public bool IsDisabled { get; set; }

    /// <summary>Indicates whether or not the Runner is disabled</summary>
    public bool AllowMultipleInstances { get; set; }

    /// <summary>Setting this to 'true' allows this Runner to be selected for deployment by any Module in the system.</summary>
    public bool IsSuppliedToAllModules { get; set; } = false;
}
