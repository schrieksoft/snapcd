// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Contracts.Dto.SourceRefresherPreselections;

/// <summary>DTO for creating a new SourceRefresherPreselection (POST operations).</summary>
public class SourceRefresherPreselectionCreateDto
{
    /// <summary>ID of the Runner to preselect as 'refresher' for the given Source URL. Messages requesting a source refresh will always be sent to this Runner's</summary>
    public Guid RunnerId { get; set; }

    /// <summary>Name a specific runner instance to select (should unique identify the the instance). Use this if you have enabled multiple instances on your runner, but want all refresh requests for this source to go to a specific instance.</summary>
    public string? RunnerInstanceName { get; set; }

    /// <summary>Unique Source URL to which a Runner (or specific Runner within the Runner based on `runner_instance_name`) is assigned as the preselected 'refresher'.</summary>
    public string SourceUrl { get; set; } = null!;
}
