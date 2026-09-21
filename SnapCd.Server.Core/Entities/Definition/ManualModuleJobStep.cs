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
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// One task dispatched for one Module within a manual job. This is the only progress record: a
/// fan-in decision ("has every participant finished this stage") is a query over these rows, and
/// the same query is what the job page renders, so the saga carries no counters.
/// </summary>
public class ManualModuleJobStep : AuditBase, IEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    public Guid JobId { get; set; }

    /// <summary>Set for transfer jobs, so a Transfer's steps can be read without walking its jobs.</summary>
    public Guid? TransferId { get; set; }

    /// <summary>Which participant this step ran against; a split job's steps all name its one Module.</summary>
    public Guid ModuleId { get; set; }

    [MaxLength(100)] public string Task { get; set; } = null!;

    public int Attempt { get; set; }

    public ManualJobStepStatus Status { get; set; }

    public int? ExitCode { get; set; }

    [MaxLength(255)] public string? ErrorHeader { get; set; }

    [MaxLength(16000)] public string? Error { get; set; }

    [MaxLength(255)] public string? RunnerInstanceName { get; set; }

    /// <summary>
    /// Hash of everything this step's result depends on: the ref as checked out, the resolved input
    /// values, the backend state's serial and lineage, the fragment, and the outputs threaded in
    /// from producers. A proof is reusable while this is unchanged, and a retry re-runs exactly the
    /// steps whose inputs moved.
    /// </summary>
    [MaxLength(64)] public string? InputKey { get; set; }

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }

    public string? Log { get; set; }

    [JsonIgnore] public ManualModuleJob Job { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleId;
    }
}
