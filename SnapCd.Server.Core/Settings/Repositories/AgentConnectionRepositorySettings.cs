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
/// Repository settings for AgentConnection entity.
/// Events are disabled since connections are runtime state, not configuration changes.
/// </summary>
public class AgentConnectionRepositorySettings : IEntitySettings
{
    /// <summary>When true (default), publish a CreatedEvent on the bus when a AgentConnection is created.</summary>
    public bool EmitCreateEvents { get; set; } = false;
    /// <summary>When true (default), publish an UpdatedEvent on the bus when a AgentConnection is updated.</summary>
    public bool EmitUpdateEvents { get; set; } = false;
    /// <summary>When true (default), publish a DeletedEvent on the bus when a AgentConnection is deleted.</summary>
    public bool EmitDeleteEvents { get; set; } = false;
    /// <summary>Time-to-live for emitted events from this entity. Defaults to 30 minutes — increase only for entities whose downstream consumers might be offline long enough to miss the default window.</summary>
    public TimeSpan EventTtl { get; set; } = TimeSpan.FromMinutes(30);
}
