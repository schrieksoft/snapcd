// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;

namespace SnapCd.Server.Core.Events.Gatekeeping;

public class GatekeepingJobRequestedBase
{
    public required Guid ModuleId { get; set; }

    public required Guid OrganizationId { get; set; }

    public Guid? JobId { get; set; }

    //public Guid CorrelationId { get; set; }
    public DesiredStateHeadline DesiredStateHeadline { get; set; }
    public bool SetNewDesiredState { get; set; }

    public string? DefinitiveRevision { get; set; }

    public string? RunnerInstanceNameOverride { get; set; }
}