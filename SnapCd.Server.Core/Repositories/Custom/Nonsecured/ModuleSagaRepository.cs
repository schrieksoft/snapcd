// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using MassTransit;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Misc.Exceptions;

namespace SnapCd.Server.Core.Repositories.Custom.Nonsecured;

public class ModuleSagaRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IBus bus)
{
    public ModuleSagaRepository Create()
    {
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleSagaRepository(dbContext, bus);
    }
}

public class ModuleSagaRepository : IDisposable
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IBus _bus;

    public ModuleSagaRepository(SnapCdDbContext dbContext, IBus bus)
    {
        _dbContext = dbContext;
        _bus = bus;
    }

    public virtual async Task<ModuleSaga> Get(Guid correlationId, Guid organizationId)
    {
        var query = _dbContext.Set<ModuleSaga>().AsQueryable();

        var entity = await query
            .FirstOrDefaultAsync(i => i.CorrelationId == correlationId && i.OrganizationId == organizationId);

        if (entity != null)
            return entity;

        // The saga is written in the same transaction as the module, so its absence means the row
        // was lost rather than never created. Without one the state machine correlates no events
        // and the module is inert, so it is restored in the state creation would have given it.
        var module = await _dbContext.Modules
            .FirstOrDefaultAsync(m => m.Id == correlationId && m.OrganizationId == organizationId);

        if (module == null)
            throw new EntityNotFoundException(
                $"{nameof(ModuleSaga)} with CorrelationId {correlationId} in Organization {organizationId} not found.");

        entity = new ModuleSaga
        {
            CorrelationId = correlationId,
            OrganizationId = organizationId,
            RowVersion = [],
            CurrentState = "Gatekeeping",
            DesiredStateHeadline = await LastCompletedDesiredState(correlationId, organizationId),
            QueuedDesiredStateHeadline = null
        };

        _dbContext.Set<ModuleSaga>().Add(entity);
        await _dbContext.SaveChangesAsync();

        return entity;
    }

    /// <summary>
    /// What the module was last known to be driving towards, so a restored saga does not assert an
    /// intent the module never had. Null where it has never completed a job.
    /// </summary>
    private async Task<DesiredStateHeadline?> LastCompletedDesiredState(Guid moduleId, Guid organizationId)
    {
        var lastHeadline = await _dbContext.ModuleJobs
            .Where(j => j.ModuleId == moduleId && j.OrganizationId == organizationId && j.ActualStateHeadline != null)
            .OrderByDescending(j => j.TimestampEnd)
            .Select(j => j.ActualStateHeadline)
            .FirstOrDefaultAsync();

        return lastHeadline switch
        {
            ActualStateHeadline.Destroyed => DesiredStateHeadline.Destroyed,
            null => null,
            _ => DesiredStateHeadline.Applied
        };
    }

    public virtual async Task<ModuleSaga> SetPaused(
        Guid correlationId,
        Guid organizationId,
        bool paused,
        Guid principalId,
        string? reason)
    {
        var entity = await Get(correlationId, organizationId);

        entity.Paused = paused;
        entity.PausedBy = paused ? principalId : null;
        entity.PausedAt = paused ? DateTime.UtcNow : null;
        entity.PauseReason = paused ? reason : null;

        await _dbContext.SaveChangesAsync();

        await _bus.Publish(new ModuleSagaModifiedEvent { ModuleId = correlationId, OrganizationId = organizationId });

        // Unpausing re-drives the gatekeeper rather than forcing execution: it re-checks dependencies,
        // a running job and runner availability, so parked work may stay queued for another reason.
        if (!paused)
            await _bus.Publish(new ModuleDependencyCheckRequested { ModuleId = correlationId, OrganizationId = organizationId });

        return entity;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}