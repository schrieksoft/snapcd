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
/// DTO for AgentConnection responses (GET operations).
/// Represents an active agent connection to a specific server instance.
/// </summary>
public class AgentConnectionReadDto : IDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid AgentId { get; set; }
    public string InstanceName { get; set; } = null!;
    public string ConnectionId { get; set; } = null!;
    public Guid ServerInstanceId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime ModifiedDateTime { get; set; }
}
