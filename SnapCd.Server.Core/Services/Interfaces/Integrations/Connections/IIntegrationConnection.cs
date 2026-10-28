// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Server.Core.Services.Integrations.Connections;

/// <summary>
/// Marker for a typed integration connection (credentials + config). The whole object is serialized as one
/// blob into the secret backend; secret fields are masked on display by the type's codec.
/// </summary>
public interface IIntegrationConnection
{
}

/// <summary>Slack connection (bot mode) — a bot token + the default channel to post to.</summary>
public sealed record SlackConnection : IIntegrationConnection
{
    /// <summary>Bot token (<c>xoxb-…</c>). Secret — masked on display.</summary>
    public string BotToken { get; init; } = string.Empty;

    /// <summary>Channel id or name to post to by default (non-secret).</summary>
    public string DefaultChannel { get; init; } = string.Empty;
}
