// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using SnapCd.Contracts;

namespace SnapCd.Server.Core.Events.Jobs.Module;

/// <summary>
/// What the shared cancel machinery needs from a cancel request. Each job family publishes its own
/// event so that a cancel only ever reaches sagas of that family.
/// </summary>
public interface ICancelRequest
{
    Guid CorrelationId { get; set; }

    Guid OrganizationId { get; set; }

    CancellationType CancellationType { get; set; }
}
