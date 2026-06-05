// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Settings;

/// <summary>
/// Distributed-cache and SignalR-backplane configuration. Single-replica deployments can leave
/// the default InMemory provider; multi-replica deployments must use Redis so cache hits and
/// SignalR group state are shared across replicas.
/// </summary>
public class CachingSettings
{
    /// <summary>
    /// Cache backing. <c>InMemory</c> (default) keeps everything in-process — fine for single-replica
    /// deployments. <c>Redis</c> uses a shared Redis instance as both the cache provider and the
    /// SignalR backplane, required when running more than one Server replica.
    /// </summary>
    public CacheProvider Provider { get; set; } = CacheProvider.InMemory;

    /// <summary>
    /// Connection string for the Redis instance. Required when <see cref="Provider"/> is Redis;
    /// ignored when InMemory. Sensitive in production — source via the External Settings provider
    /// when the Redis instance is auth-protected.
    /// </summary>
    public string? ConnectionString { get; set; }
}
