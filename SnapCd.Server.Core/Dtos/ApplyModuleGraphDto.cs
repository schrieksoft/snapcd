// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Dtos;

public class ApplyModuleGraphDto
{
    public Guid RootModuleId { get; set; }
    public List<ApplyModuleNodeDto> NodeStates { get; set; } = new();
    public int TotalModuleCount { get; set; }
    public int TotalStages { get; set; }
}

public class ApplyModuleNodeDto
{
    public Guid ModuleId { get; set; }
    public string DisplayName { get; set; } = null!;
    public int Stage { get; set; }
    public ActualStateHeadline? ActualState { get; set; }

    /// <summary>
    /// Modules that depend on this module (must wait for this to be applied)
    /// </summary>
    public List<string> DependentModules { get; set; } = new();

    /// <summary>
    /// Modules this module depends on (must be applied before this)
    /// </summary>
    public List<string> DependencyModules { get; set; } = new();
}