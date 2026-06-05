// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Agent.Configuration;

/// <summary>
/// Coordinates of the Snap CD Server the Agent connects to. Bound from the <c>Server</c>
/// section of <c>appsettings.json</c>.
/// </summary>
public sealed class ServerSettings
{
    public const string SectionName = "Server";

    /// <summary>
    /// Base URL of the Snap CD Server, including scheme and port. The Agent opens its SignalR
    /// connection to <c>{Url}/agenthub</c>, fetches the MCP surface from <c>{Url}/mcp</c>, and
    /// obtains JWTs from <c>{Url}/connect/token</c>.
    /// </summary>
    public string Url { get; set; } = null!;
}
