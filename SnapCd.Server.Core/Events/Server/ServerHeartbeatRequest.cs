// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Events.Server;

/// <summary>
/// Request message to check if a specific server instance still has an active runner connection.
/// Sent to a server's fanout endpoint to verify connection validity during duplicate detection.
/// </summary>
public class ServerHeartbeatRequest
{
    public Guid ServerInstanceId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RunnerId { get; set; }
    public string InstanceName { get; set; } = null!;
}
