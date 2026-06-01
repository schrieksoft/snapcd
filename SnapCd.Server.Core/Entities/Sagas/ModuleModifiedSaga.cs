// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using MassTransit;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Entities.Sagas;

public class ModuleModifiedSaga : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public Guid OrganizationId { get; set; }
    [MaxLength(100)] public string CurrentState { get; set; } = null!;
    public DateTime? LastUpdated { get; set; }
    public Guid? TimeoutTokenId { get; set; }

    public virtual Module? Module { get; set; }

    // Required by EF Core
    public byte[] RowVersion { get; set; } = null!;
}