// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MassTransit;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Entities.Sagas;

public class ModuleSaga : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    [MaxLength(100)] public string CurrentState { get; set; } = null!;

    public DesiredStateHeadline? DesiredStateHeadline { get; set; }

    public DesiredStateHeadline? QueuedDesiredStateHeadline { get; set; }

    public QueuedReason? QueuedReason { get; set; }

    [MaxLength(255)] public string? DesiredDefinitiveRevision { get; set; }

    [JsonIgnore] public Module Module { get; set; } = null!;

    public int? ActualResourceCount { get; set; }

    public Guid? DriftCheckScheduleTokenId { get; set; }
}