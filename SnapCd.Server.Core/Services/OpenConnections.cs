// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace SnapCd.Server.Core.Services;

/// <summary>
/// The connections this server process holds open on <typeparamref name="THub"/>. A connection row is only
/// dispatched to by the server that owns it, so that server can tell from here, without a round trip, whether
/// the socket behind a row is still open.
/// </summary>
public class OpenConnections<THub> where THub : Hub
{
    private readonly ConcurrentDictionary<string, byte> _connectionIds = new();

    public void Add(string connectionId) => _connectionIds.TryAdd(connectionId, 0);

    public void Remove(string connectionId) => _connectionIds.TryRemove(connectionId, out _);

    public bool Contains(string connectionId) => _connectionIds.ContainsKey(connectionId);
}
