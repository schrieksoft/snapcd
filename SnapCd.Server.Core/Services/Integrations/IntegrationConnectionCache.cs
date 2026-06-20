// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Caching.Memory;

namespace SnapCd.Server.Core.Services.Integrations;

/// <summary>
/// Per-instance, in-process cache of an integration's **connection JSON** (the credentials blob) so the
/// high-frequency dispatch/send path doesn't read the secret backend on every event. Strictly
/// <see cref="IMemoryCache"/> (never distributed); entries are invalidated cross-instance by the fanout
/// <c>Integration*CacheInvalidationConsumer</c>s on integration CRUD (so a rotated token isn't served
/// stale), with a 15-minute absolute TTL as a backstop. The cached value contains secrets — it lives only
/// in process memory, the same trust boundary as using the credential to send.
/// </summary>
public sealed class IntegrationConnectionCache(IMemoryCache cache)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(15);

    private static string Key(Guid integrationId) => $"integration:connection:{integrationId}";

    /// <summary>Read-through: returns the cached connection JSON, or invokes <paramref name="factory"/>
    /// (a fresh secret-backend read) on a miss and caches the result.</summary>
    public async Task<string?> GetOrCreateAsync(Guid integrationId, Func<Task<string?>> factory)
        => await cache.GetOrCreateAsync(Key(integrationId), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;
            return factory();
        });

    public void Evict(Guid integrationId) => cache.Remove(Key(integrationId));
}
