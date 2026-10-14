// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services;

/// <summary>
/// Records which task a ripple probe is running on which connection, and how recently. Parallel to
/// RunnerConnectionStateMigrationService: the heartbeat reads it to decide whether the runner is
/// still alive, and a probe is in neither ModuleJobs nor StateMigrationJobs, so it cannot use
/// either of their tables.
/// </summary>
public class RunnerConnectionRippleProbeService
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public RunnerConnectionRippleProbeService(IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task CreateOrUpdate(Guid organizationId, Guid rippleProbeJobId, string taskName, Guid runnerId, string? runnerInstanceName)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var connection = await db.RunnerConnections
            .Where(rc => rc.OrganizationId == organizationId &&
                         rc.RunnerId == runnerId &&
                         rc.InstanceName == runnerInstanceName)
            .Select(rc => new { rc.Id })
            .FirstOrDefaultAsync();

        if (connection == null)
            throw new InvalidOperationException(
                $"No active runner connection found for runner {runnerId} with instance name '{runnerInstanceName}' in organization {organizationId}");

        var existing = await db.RunnerConnectionRippleProbes
            .Where(r => r.OrganizationId == organizationId &&
                        r.RippleProbeJobId == rippleProbeJobId &&
                        r.RunnerConnection.RunnerId == runnerId &&
                        r.RunnerConnection.InstanceName == runnerInstanceName)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            existing.RunnerConnectionId = connection.Id;
            existing.TaskName = taskName;
            existing.ModifiedDateTime = DateTime.UtcNow;
        }
        else
        {
            db.RunnerConnectionRippleProbes.Add(new RunnerConnectionRippleProbe
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                RunnerConnectionId = connection.Id,
                RippleProbeJobId = rippleProbeJobId,
                TaskName = taskName
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>When the runner last reported on this probe, or null if it never has.</summary>
    public async Task<DateTime?> LastReportedAt(Guid organizationId, Guid rippleProbeJobId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        return await db.RunnerConnectionRippleProbes
            .Where(r => r.OrganizationId == organizationId && r.RippleProbeJobId == rippleProbeJobId)
            .Select(r => (DateTime?)r.ModifiedDateTime)
            .FirstOrDefaultAsync();
    }
}
