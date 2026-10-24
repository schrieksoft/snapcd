// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.ModuleHold;

/// <summary>The cache key of one hold: on a module, on every module in a namespace, or on every module in a stack.</summary>
public readonly record struct ModuleHoldKey(string Value)
{
    public static ModuleHoldKey Module(Guid organizationId, Guid moduleId) => new($"modulehold:{organizationId:N}:module:{moduleId:N}");

    public static ModuleHoldKey Namespace(Guid organizationId, Guid namespaceId) => new($"modulehold:{organizationId:N}:namespace:{namespaceId:N}");

    public static ModuleHoldKey Stack(Guid organizationId, Guid stackId) => new($"modulehold:{organizationId:N}:stack:{stackId:N}");
}

/// <summary>A hold owned by a principal until <see cref="ExpiresAt"/>, which each heartbeat moves on.</summary>
public sealed record ModuleHoldEntry(Guid PrincipalId, int ExpirySeconds, DateTime ExpiresAt);

/// <summary>Whether a module is held right now, and if so until when it is worth checking again.</summary>
public sealed record ModuleHoldState(bool Held, DateTime? ExpiresAt)
{
    public static readonly ModuleHoldState NotHeld = new(false, null);
}

/// <summary>
/// Module holds, kept only in the distributed cache. A module is held by a hold on itself, on its namespace or on its
/// stack. Anything missing from the cache counts as not held, so losing it releases modules and never strands them.
/// </summary>
public class ModuleHoldStore
{
    private readonly IDistributedCache _cache;
    private readonly ModuleHoldSettings _settings;
    private readonly TimeProvider _time;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public ModuleHoldStore(IDistributedCache cache, IOptions<ModuleHoldSettings> settings, TimeProvider time, IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _cache = cache;
        _settings = settings.Value;
        _time = time;
        _dbContextFactory = dbContextFactory;
    }

    public ModuleHoldSettings Settings => _settings;

    public DateTime Now => _time.GetUtcNow().UtcDateTime;

    public async Task<ModuleHoldEntry?> Get(ModuleHoldKey key)
    {
        var json = await _cache.GetStringAsync(key.Value);
        return json == null ? null : JsonSerializer.Deserialize<ModuleHoldEntry>(json);
    }

    /// <summary>Holds for <paramref name="expirySeconds"/> from now, owned by <paramref name="principalId"/>.</summary>
    // Kept for twice its expiry, so a check landing just after ExpiresAt can still read it.
    public Task Hold(ModuleHoldKey key, Guid principalId, int expirySeconds) =>
        _cache.SetStringAsync(
            key.Value,
            JsonSerializer.Serialize(new ModuleHoldEntry(principalId, expirySeconds, Now.AddSeconds(expirySeconds))),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2 * expirySeconds) });

    public Task Remove(ModuleHoldKey key) => _cache.RemoveAsync(key.Value);

    /// <summary>Held while a hold on the module, its namespace or its stack is live; until the latest of those.</summary>
    public async Task<ModuleHoldState> GetHoldState(Guid organizationId, Guid moduleId)
    {
        var keys = new List<ModuleHoldKey> { ModuleHoldKey.Module(organizationId, moduleId) };

        await using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            var parents = await dbContext.Modules
                .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
                .Select(m => new { m.NamespaceId, m.Namespace.StackId })
                .FirstOrDefaultAsync();
            if (parents != null)
            {
                keys.Add(ModuleHoldKey.Namespace(organizationId, parents.NamespaceId));
                keys.Add(ModuleHoldKey.Stack(organizationId, parents.StackId));
            }
        }

        DateTime? expiresAt = null;
        foreach (var key in keys)
        {
            var hold = await Get(key);
            if (hold != null && Now < hold.ExpiresAt && (expiresAt == null || hold.ExpiresAt > expiresAt))
                expiresAt = hold.ExpiresAt;
        }

        return expiresAt == null ? ModuleHoldState.NotHeld : new ModuleHoldState(true, expiresAt);
    }
}
