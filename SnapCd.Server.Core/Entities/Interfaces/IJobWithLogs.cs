// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


namespace SnapCd.Server.Core.Entities.Interfaces;

/// <summary>
/// A job whose runner logs are stored on the row. Closed over by JobLogService so the logic is
/// written once while each caller names the table it means.
/// </summary>
public interface IJobWithLogs
{
    Guid Id { get; set; }
    Guid OrganizationId { get; set; }
    string? Logs { get; set; }

    /// <summary>The runner this job was dispatched to, which is what authorizes one writing to it.</summary>
    Guid? SelectedRunnerId { get; set; }
}
