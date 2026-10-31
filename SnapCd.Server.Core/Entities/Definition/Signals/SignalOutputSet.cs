// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition.Signals;

/// <summary>
/// What one probe reported: this Module's outputs as they will be, with a value that may be
/// unknown. The counterpart of <see cref="OutputSet"/> for a prediction rather than a fact, kept
/// apart from it so a prediction can never be resolved as a real input.
/// </summary>
public class SignalOutputSet : AuditBase, IEntity, IModuleChild
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid ModuleId { get; set; }

    /// <summary>The wave this was produced in. Signals outlive nothing else.</summary>
    public Guid ProbeWaveId { get; set; }

    /// <summary>The job whose probe step wrote it.</summary>
    public Guid ModuleJobId { get; set; }

    public long Timestamp { get; set; }

    [MaxLength(1024)] public string Checksum { get; set; } = null!;

    public List<SignalOutput> SignalOutputs { get; set; } = new();

    [JsonIgnore] public ModuleJob ModuleJob { get; set; } = null!;
    [JsonIgnore] public ProbeWave ProbeWave { get; set; } = null!;
    [JsonIgnore] public Module Module { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleId;
    }
}
