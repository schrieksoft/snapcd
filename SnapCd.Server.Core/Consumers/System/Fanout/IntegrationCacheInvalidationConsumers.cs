// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Services.Integrations;

namespace SnapCd.Server.Core.Consumers.System.Fanout;

/// <summary>
/// Fanout (every instance) — evicts an integration's cached credentials when it is updated, so the next
/// dispatch re-reads the secret backend (a rotated token isn't served stale). Create needs no eviction
/// (nothing is cached yet).
/// </summary>
public class IntegrationUpdatedCacheInvalidationConsumer : IConsumer<IntegrationUpdatedEvent>
{
    private readonly IntegrationConnectionCache _cache;

    public IntegrationUpdatedCacheInvalidationConsumer(IntegrationConnectionCache cache) => _cache = cache;

    public Task Consume(ConsumeContext<IntegrationUpdatedEvent> context)
    {
        _cache.Evict(context.Message.Data.Id);
        return Task.CompletedTask;
    }
}

/// <summary>Fanout (every instance) — evicts an integration's cached credentials when it is deleted.</summary>
public class IntegrationDeletedCacheInvalidationConsumer : IConsumer<IntegrationDeletedEvent>
{
    private readonly IntegrationConnectionCache _cache;

    public IntegrationDeletedCacheInvalidationConsumer(IntegrationConnectionCache cache) => _cache = cache;

    public Task Consume(ConsumeContext<IntegrationDeletedEvent> context)
    {
        _cache.Evict(context.Message.Data.Id);
        return Task.CompletedTask;
    }
}
