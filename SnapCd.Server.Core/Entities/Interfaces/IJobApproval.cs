// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;

namespace SnapCd.Server.Core.Entities.Interfaces;

/// <summary>
/// The decision itself, without the job it belongs to. The two job families keep their own
/// approval tables and foreign keys; what an approvals view reads is identical across both.
/// </summary>
public interface IJobApproval
{
    Guid Id { get; }
    Guid OrganizationId { get; }
    Guid PrincipalId { get; }
    PrincipalDiscriminator PrincipalDiscriminator { get; }
    Guid? AgentId { get; }
    string? Reason { get; }
    DateTime DecisionDateTime { get; }
    bool Declined { get; }
}
