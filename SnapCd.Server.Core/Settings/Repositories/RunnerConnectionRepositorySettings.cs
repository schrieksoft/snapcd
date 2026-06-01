// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Settings.Repositories;

/// <summary>
/// Repository settings for RunnerConnection entity.
/// Events are disabled since connections are runtime state, not configuration changes.
/// </summary>
public class RunnerConnectionRepositorySettings : IEntitySettings
{
    public bool EmitCreateEvents { get; set; } = false;
    public bool EmitUpdateEvents { get; set; } = false;
    public bool EmitDeleteEvents { get; set; } = false;
    public TimeSpan EventTtl { get; set; } = TimeSpan.FromMinutes(30);
}
