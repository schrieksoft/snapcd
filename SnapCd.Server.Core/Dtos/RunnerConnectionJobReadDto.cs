// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos;

/// <summary>
/// DTO for RunnerConnectionJob responses (GET operations).
/// Represents the association between a runner connection and a module job.
/// </summary>
public class RunnerConnectionJobReadDto : IDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RunnerConnectionId { get; set; }
    public Guid ModuleJobId { get; set; }
    public string TaskName { get; set; } = null!;
    public DateTime CreatedDateTime { get; set; }
    public DateTime ModifiedDateTime { get; set; }
}