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
/// A read-only plan of one Module, run with an upstream Module's changed outputs supplied as the
/// values they will become, to report what the change does downstream. Kept separate from
/// ModuleJob because that entity's unique index on module and start timestamp admits one job per
/// module at a time, and this one runs alongside whatever the module is already doing.
/// </summary>
public class RippleProbeJob : AuditBase, IEntity, IModuleChild, IJobWithLogs
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>The Module being planned: the consumer of the changed outputs.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>The Module whose outputs changed, which is what prompted this plan.</summary>
    public Guid UpstreamModuleId { get; set; }

    [JsonIgnore] public Module UpstreamModule { get; set; } = null!;

    /// <summary>
    /// The ModuleJob whose plan reported the change, when one did. Null for a plan requested by
    /// hand, which is the only way a ripple probe starts until Apply and Destroy trigger them.
    /// </summary>
    public Guid? UpstreamModuleJobId { get; set; }

    public int JobNumber { get; set; }

    public DateTimeOffset TimestampStart { get; set; }
    public DateTimeOffset? TimestampEnd { get; set; }

    public ExecutionStatus Status { get; set; }

    /// <summary>
    /// The variable values this plan was handed, keyed by this Module's own variable names. Opaque
    /// to the server: cty values carrying unknowns, which only the runner encodes and decodes.
    /// </summary>
    public string InputsJson { get; set; } = null!;

    /// <summary>
    /// What the plan said about each of this Module's outputs, keyed by its own output names.
    /// Opaque to the server, as <see cref="InputsJson"/> is.
    /// </summary>
    public string? SignalsJson { get; set; }

    /// <summary>
    /// Set while the job is parked because the runner it is pinned to is away. Without it the job
    /// reads as Running, which is indistinguishable from working.
    /// </summary>
    public bool? WaitingForRunner { get; set; }
    /// <summary>
    /// The runner this job was dispatched to. Recorded on the job rather than only on the saga,
    /// which is deleted when the job finishes, and carried without a foreign key so it survives
    /// the runner being removed.
    /// </summary>
    public Guid? SelectedRunnerId { get; set; }

    public ServerSideStep? FailedOnServerSideStep { get; set; }

    [MaxLength(255)] public string? ServerSideErrorHeader { get; set; }

    [MaxLength(16000)] public string? ServerSideError { get; set; }

    public string? Logs { get; set; }

    [JsonIgnore] public Module Module { get; set; } = null!;
    [JsonIgnore] public virtual Organization Organization { get; set; } = null!;

    public Guid ParentId()
    {
        return ModuleId;
    }
}
