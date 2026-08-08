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
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Services.MaintenanceMode;

public interface IMaintenanceModeService
{
    Task<bool> IsActiveAsync();
    Task<Entities.Definition.MaintenanceMode?> GetAsync();
    Task<MaintenanceModeStatus> GetStatusAsync();
    Task SyncCacheAsync();
    Task EnableAsync(Guid enabledBy, string? reason);
    Task AdvanceToAsync(MaintenancePhase phase, IReadOnlyList<MaintenancePhase>? skipped = null);
    Task RecordPhaseActionAsync(string summary);
    Task DisableAsync();
}

public record MaintenanceModeStatus(Entities.Definition.MaintenanceMode? Database, Entities.Definition.MaintenanceMode? Cache, bool InSync);

/// <summary>
/// DB-backed maintenance flag with a write-through shared cache and no expiry: a flip writes
/// the database and then sets the cache value in the same operation, so with the Redis
/// provider every replica sees the change on its next check — no polling, no TTL. A cache
/// miss reads through from the database and refills. If the cache cannot be set during a
/// flip, the key is removed instead (a miss stays correct) and the failure is surfaced to
/// the operator; the UI verifies via GetStatusAsync and can repair via SyncCacheAsync.
/// Writes go through the DbContext directly — the gate must never block its own switch.
/// Multiple instances on the InMemory provider are unsupported: their caches never converge.
/// </summary>
public class MaintenanceModeService : IMaintenanceModeService
{
    private const string CacheKey = "maintenance-mode";

    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IDistributedCache _cache;
    private readonly ILogger<MaintenanceModeService> _logger;

    public MaintenanceModeService(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IDistributedCache cache,
        ILogger<MaintenanceModeService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsActiveAsync()
        => (await GetCachedAsync())?.Enabled ?? false;

    public async Task<Entities.Definition.MaintenanceMode?> GetAsync()
        => await GetCachedAsync();

    public async Task EnableAsync(Guid enabledBy, string? reason)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var row = await GetOrCreateRowAsync(db);
        row.Enabled = true;
        row.EnabledBy = enabledBy;
        row.EnabledAt = DateTime.UtcNow;
        row.Reason = reason;
        row.Phase = MaintenancePhase.Draining;
        row.PhaseEnteredAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await WriteThroughAsync(row);
        _logger.LogWarning("Maintenance mode ENABLED by {EnabledBy}: {Reason}", enabledBy, reason);
    }

    public async Task DisableAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var row = await GetOrCreateRowAsync(db);
        row.Enabled = false;
        // The window's details describe the window that just closed; leaving them makes a closed
        // window read as though it were still open.
        row.EnabledBy = null;
        row.EnabledAt = null;
        row.Reason = null;
        row.Phase = null;
        row.PhaseEnteredAt = null;
        row.SkippedPhases = null;
        row.PhaseActionCompletedAt = null;
        row.PhaseActionSummary = null;
        row.SkippedPhases = null;
        await db.SaveChangesAsync();
        await WriteThroughAsync(row);
        _logger.LogWarning("Maintenance mode DISABLED");
    }

    public async Task AdvanceToAsync(MaintenancePhase phase, IReadOnlyList<MaintenancePhase>? skipped = null)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var row = await GetOrCreateRowAsync(db);
        if (!row.Enabled) throw new InvalidOperationException("No maintenance window is open.");

        row.Phase = phase;
        row.PhaseEnteredAt = DateTime.UtcNow;
        // The outcome recorded on the row belongs to the phase being left.
        row.PhaseActionCompletedAt = null;
        row.PhaseActionSummary = null;
        if (skipped is { Count: > 0 })
            row.SkippedPhases = string.Join(",", skipped);
        await db.SaveChangesAsync();
        await WriteThroughAsync(row);
        _logger.LogWarning("Maintenance window advanced to {Phase}{Skipped}", phase,
            skipped is { Count: > 0 } ? $", skipping {string.Join(", ", skipped)}" : "");
    }

    public async Task RecordPhaseActionAsync(string summary)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var row = await GetOrCreateRowAsync(db);
        row.PhaseActionCompletedAt = DateTime.UtcNow;
        row.PhaseActionSummary = summary;
        await db.SaveChangesAsync();
        await WriteThroughAsync(row);
    }

    public async Task<MaintenanceModeStatus> GetStatusAsync()
    {
        var dbRow = await RepairPhaseIfMissingAsync(await ReadDatabaseAsync());
        Entities.Definition.MaintenanceMode? cachedRow = null;
        var cacheReadable = false;
        try
        {
            var cached = await _cache.GetStringAsync(CacheKey);
            if (cached != null)
            {
                cachedRow = JsonSerializer.Deserialize<Entities.Definition.MaintenanceMode?>(cached);
                cacheReadable = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Maintenance-mode cache unreadable while checking status");
        }

        var inSync = cacheReadable && (dbRow?.Enabled ?? false) == (cachedRow?.Enabled ?? false);
        if (inSync) return new MaintenanceModeStatus(dbRow, cachedRow, true);

        // The database is authoritative and the cache is derived, so a divergence is repaired
        // rather than reported: only an out-of-band write can cause one, and leaving it would let
        // other replicas act on a stale flag.
        try
        {
            await WriteThroughAsync(dbRow);
            _logger.LogWarning("Maintenance flag cache diverged from the database and was rewritten");
            return new MaintenanceModeStatus(dbRow, dbRow, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Maintenance flag cache diverged and could not be rewritten");
            return new MaintenanceModeStatus(dbRow, cachedRow, false);
        }
    }

    // An open window without a phase cannot be advanced or reported on. It can only arise from a
    // row written before the phase columns existed, so it is treated as a window that has just
    // opened rather than surfaced as an error.
    private async Task<Entities.Definition.MaintenanceMode?> RepairPhaseIfMissingAsync(Entities.Definition.MaintenanceMode? row)
    {
        if (row is not { Enabled: true, Phase: null }) return row;

        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var tracked = await GetOrCreateRowAsync(db);
        tracked.Phase = MaintenancePhase.Draining;
        tracked.PhaseEnteredAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await WriteThroughAsync(tracked);
        _logger.LogWarning("Maintenance window had no phase recorded; set to {Phase}", MaintenancePhase.Draining);
        return tracked;
    }

    public async Task SyncCacheAsync()
        => await WriteThroughAsync(await ReadDatabaseAsync());

    // A failed set falls back to removing the key: a miss reads the database, so correctness
    // is preserved; a stale value with no expiry would not be.
    private async Task WriteThroughAsync(Entities.Definition.MaintenanceMode? row)
    {
        try
        {
            await _cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(row));
        }
        catch (Exception ex)
        {
            try
            {
                await _cache.RemoveAsync(CacheKey);
                _logger.LogError(ex, "Maintenance-mode cache could not be set; key removed, replicas will read the database");
            }
            catch (Exception removeEx)
            {
                _logger.LogError(removeEx, "Maintenance-mode cache could not be set or removed; verify with GetStatus and repair once the cache returns");
            }

            throw new InvalidOperationException("The maintenance flag was written to the database, but the shared cache could not be updated. Verify and retry.", ex);
        }
    }

    private async Task<Entities.Definition.MaintenanceMode?> ReadDatabaseAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Set<Entities.Definition.MaintenanceMode>().AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == Entities.Definition.MaintenanceMode.SingletonId);
    }

    private async Task<Entities.Definition.MaintenanceMode?> GetCachedAsync()
    {
        try
        {
            var cached = await _cache.GetStringAsync(CacheKey);
            if (cached != null)
                return JsonSerializer.Deserialize<Entities.Definition.MaintenanceMode?>(cached);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Maintenance-mode cache unavailable; reading the flag from the database");
        }

        var row = await ReadDatabaseAsync();

        try
        {
            await _cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(row));
        }
        catch
        {
            // The cache is an optimization; a failed refill costs one extra DB read per check.
        }

        return row;
    }

    private static async Task<Entities.Definition.MaintenanceMode> GetOrCreateRowAsync(SnapCdDbContext db)
    {
        var row = await db.Set<Entities.Definition.MaintenanceMode>()
            .SingleOrDefaultAsync(m => m.Id == Entities.Definition.MaintenanceMode.SingletonId);
        if (row == null)
        {
            row = new Entities.Definition.MaintenanceMode();
            db.Add(row);
        }

        return row;
    }
}
