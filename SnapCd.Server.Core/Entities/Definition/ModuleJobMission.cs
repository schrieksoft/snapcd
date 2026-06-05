// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// A mission applied to a single <see cref="ModuleJob"/> (a ModuleJob child, like
/// <see cref="ModuleJobApproval"/>). One row per <c>(ModuleJobId, MissionType)</c> — collapsing an
/// org- and a module-scoped mission of the same type on the same job into one logical record. Its
/// <see cref="Status"/> / <see cref="ResultSummary"/> are a projection of the latest
/// <see cref="ModuleJobMissionRun"/>; each execution attempt is a separate run row.
/// </summary>
public class ModuleJobMission : AuditBase, IEntity, ICreationTrackable
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ModuleJobId { get; set; }

    /// <summary>
    /// The mission row that triggered the first run (informational). Missions are polymorphic across
    /// the four scoped tables, so this is a loose reference (no single-table FK) — cf.
    /// <see cref="ModuleJobApproval.PrincipalId"/>. Dedup is on <see cref="MissionType"/>, not this.
    /// </summary>
    public Guid MissionId { get; set; }

    public Guid AgentId { get; set; }

    public MissionType MissionType { get; set; }

    /// <summary>
    /// Sidecar selector denormalized from the winning <c>{Scope}Mission.SidecarName</c> at row creation
    /// (see <c>MissionMatcher.GetOrCreateMissionAsync</c>). <c>null</c> means "let the agent pick its only
    /// registered sidecar" — see <c>MissionRequestBase.SidecarName</c>. Frozen at first dispatch; later
    /// edits to the source <c>{Scope}Mission</c> do not propagate here.
    /// </summary>
    [MaxLength(64)] public string? SidecarName { get; set; }

    /// <summary>Projected from the latest run.</summary>
    public MissionStatus Status { get; set; }

    /// <summary>The latest run's result text (e.g. the diagnosis).</summary>
    public string? ResultSummary { get; set; }

    public string? Error { get; set; }

    public DateTime? CompletedAt { get; set; }

    [JsonIgnore] public ModuleJob ModuleJob { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    /// <summary>One row per execution attempt (the run-tracking + concurrency-lock unit).</summary>
    [JsonIgnore] public virtual ICollection<ModuleJobMissionRun> Runs { get; set; } = new List<ModuleJobMissionRun>();

    public Guid ParentId()
    {
        return ModuleJobId;
    }
}
