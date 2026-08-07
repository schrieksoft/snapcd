// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Services.Edition;

/// <summary>
/// Whether a principal may move between organizations from the dashboard. Editions that run one
/// organization per deployment have nothing to switch to, so the selector is hidden entirely.
/// </summary>
public interface IOrganizationSwitchingPolicy
{
    bool AllowsOrganizationSwitching { get; }
}
