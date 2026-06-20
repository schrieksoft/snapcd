// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using SnapCd.Contracts;
using SnapCd.Server.Core.Services.Integrations.Connections;

namespace SnapCd.Server.Core.Services.Integrations.Codecs;

/// <summary>
/// Per-<see cref="IntegrationType"/> conversion between the stored blob, the API write payload, and the
/// redacted display view. One codec class per integration type; secret-field handling lives here so it is
/// never done generically. (Send / TestConnection are added in a later phase.)
/// </summary>
public interface IIntegrationCodec
{
    IntegrationType Type { get; }

    /// <summary>Blob (secret backend) → typed connection.</summary>
    IIntegrationConnection Deserialize(string json);

    /// <summary>Typed connection → blob.</summary>
    string Serialize(IIntegrationConnection connection);

    /// <summary>
    /// API write payload → typed connection. <paramref name="existing"/> is the currently stored connection
    /// (null on create); any secret field still holding the mask sentinel keeps its existing value.
    /// </summary>
    IIntegrationConnection FromInput(JsonElement input, IIntegrationConnection? existing);

    /// <summary>Typed connection → display-safe object with secret fields masked.</summary>
    object ToRedactedView(IIntegrationConnection connection);

    /// <summary>Required-field / format validation; empty means valid.</summary>
    IReadOnlyList<string> Validate(IIntegrationConnection connection);

    /// <summary>Deliver a rendered message. <paramref name="threadId"/> (when non-null) replies under an
    /// existing thread root (the sink message id returned by a prior send); the returned
    /// <see cref="IntegrationSendResult.MessageId"/> is the new message's id (the thread root for the first
    /// message of a sequence).</summary>
    Task<IntegrationSendResult> SendAsync(IIntegrationConnection connection, string text, string? threadId, CancellationToken ct);

    /// <summary>Verify the connection's credentials against the sink without sending a visible message.</summary>
    Task<IntegrationTestResult> TestConnectionAsync(IIntegrationConnection connection, CancellationToken ct);
}

public sealed record IntegrationSendResult(bool Success, string? MessageId, string? Error);

public sealed record IntegrationTestResult(bool Success, string? Error);

/// <summary>Resolves the codec for an <see cref="IntegrationType"/>.</summary>
public interface IIntegrationCodecRegistry
{
    IIntegrationCodec Get(IntegrationType type);
}

public sealed class IntegrationCodecRegistry : IIntegrationCodecRegistry
{
    private readonly IReadOnlyDictionary<IntegrationType, IIntegrationCodec> _codecs;

    public IntegrationCodecRegistry(IEnumerable<IIntegrationCodec> codecs)
        => _codecs = codecs.ToDictionary(c => c.Type);

    public IIntegrationCodec Get(IntegrationType type)
        => _codecs.TryGetValue(type, out var codec)
            ? codec
            : throw new NotSupportedException($"No integration codec registered for type '{type}'.");
}
