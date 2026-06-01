// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using MassTransit;

namespace SnapCd.Server.Core.Entities.Sagas.Base;

public class JobSagaBase : SagaStateMachineInstance
{
    [MaxLength(255)] public string CurrentState { get; set; } = null!;

    public byte[] RowVersion { get; set; } = null!;

    [MaxLength(500)] public string? ResponseAddress { get; set; }

    public Guid? RequestId { get; set; }

    public Guid? GracefulCancellationRequestId { get; set; }

    public Guid? KillCancellationRequestId { get; set; }

    public Guid? HeartbeatRequestId { get; set; }

    public Guid? HeartbeatScheduleTokenId { get; set; }

    public Guid? ApprovalTimeoutScheduleTokenId { get; set; }

    public int? ApprovalTimeoutMinutes { get; set; }

    public bool IsCompleted { get; set; }

    public int Version { get; set; } // For optimistic concurrency control

    public Guid ModuleId { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid RunnerId { get; set; }

    [MaxLength(255)] public string RunnerName { get; set; } = null!;


    [MaxLength(255)] public string? RunnerInstanceName { get; set; }

    public string DeclaredJson { get; set; } = null!;
    public Guid CorrelationId { get; set; }

    public bool IsApproved { get; set; }

    public bool IsDeclined { get; set; }

    [MaxLength(255)] public string? PreviousStateBeforeWaiting { get; set; }

    [MaxLength(255)] public string? PreviousStateBeforeCancelling { get; set; }

    public DateTime? WaitingSince { get; set; }

    public Guid? ServerInstanceId { get; set; }

    [MaxLength(255)] public string? DefinitiveRevision { get; set; }
}