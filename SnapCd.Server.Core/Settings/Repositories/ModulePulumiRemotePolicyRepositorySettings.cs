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
/// Repository event emission settings for ModulePulumiRemotePolicy entities.
/// </summary>
public class ModulePulumiRemotePolicyRepositorySettings : IEntitySettings
{
    /// <summary>When true (default), publish a CreatedEvent on the bus when a ModulePulumiRemotePolicy is created.</summary>
    public bool EmitCreateEvents { get; set; } = true;
    /// <summary>When true (default), publish an UpdatedEvent on the bus when a ModulePulumiRemotePolicy is updated.</summary>
    public bool EmitUpdateEvents { get; set; } = true;
    /// <summary>When true (default), publish a DeletedEvent on the bus when a ModulePulumiRemotePolicy is deleted.</summary>
    public bool EmitDeleteEvents { get; set; } = true;
    /// <summary>Time-to-live for emitted events from this entity. Defaults to 30 minutes — increase only for entities whose downstream consumers might be offline long enough to miss the default window.</summary>
    public TimeSpan EventTtl { get; set; } = TimeSpan.FromMinutes(30);
}
