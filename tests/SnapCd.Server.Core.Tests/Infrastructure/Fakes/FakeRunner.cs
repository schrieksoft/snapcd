// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SnapCd.Contracts;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Hubs.Handlers;
using SnapCd.Server.Core.Services.CallerContext;
using CallerCtx = SnapCd.Server.Core.Services.CallerContext.CallerContext;

namespace SnapCd.Server.Core.Tests.Infrastructure.Fakes;

/// <summary>
/// Stands in for a connected runner without hosting SignalR. The hub methods only authorize and
/// delegate to these handlers, so driving the handlers exercises everything downstream — the
/// real consumers, sagas, repositories and gates — with the runner's caller scope applied, which
/// is what the maintenance window's exemptions depend on.
/// </summary>
public class FakeRunner
{
    private readonly IBus _bus;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public FakeRunner(IBus bus, IDbContextFactory<SnapCdDbContext> dbContextFactory, Guid organizationId, Guid runnerId, string instanceName)
    {
        _bus = bus;
        _dbContextFactory = dbContextFactory;
        OrganizationId = organizationId;
        RunnerId = runnerId;
        InstanceName = instanceName;
    }

    public Guid OrganizationId { get; }
    public Guid RunnerId { get; }
    public string InstanceName { get; }
    public Guid ServerInstanceId { get; } = Guid.NewGuid();

    /// <summary>Registers the connection row the server looks up when dispatching work.</summary>
    public async Task ConnectAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var existing = await db.RunnerConnections.FirstOrDefaultAsync(rc =>
            rc.RunnerId == RunnerId && rc.OrganizationId == OrganizationId && rc.InstanceName == InstanceName);
        if (existing == null)
            db.RunnerConnections.Add(new RunnerConnection
            {
                Id = Guid.NewGuid(),
                OrganizationId = OrganizationId,
                RunnerId = RunnerId,
                InstanceName = InstanceName,
                SignalRConnectionId = $"{InstanceName}-connection",
                ServerInstanceId = ServerInstanceId
            });
        else
            existing.ServerInstanceId = ServerInstanceId;

        await db.SaveChangesAsync();
    }

    public async Task DisconnectAsync()
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        var existing = await db.RunnerConnections.Where(rc =>
            rc.RunnerId == RunnerId && rc.OrganizationId == OrganizationId && rc.InstanceName == InstanceName).ToListAsync();
        db.RunnerConnections.RemoveRange(existing);
        await db.SaveChangesAsync();
    }

    /// <summary>The per-task progress ping that keeps the job's heartbeat check satisfied.</summary>
    public async Task ReportRunningTaskAsync(Guid jobId, string taskName)
    {
        using var _ = CallerCtx.Begin(CallerKind.Runner);
        await new ReportRunningTaskHandler(NullLogger<ReportRunningTaskHandler>.Instance, _bus)
            .Report(OrganizationId, jobId, taskName, RunnerId, InstanceName);
    }

    public async Task CompletePlanAsync(Guid jobId, int totalChanged = 1)
    {
        using var _ = CallerCtx.Begin(CallerKind.Runner);
        await new PlanHandler(NullLogger<PlanHandler>.Instance, _bus)
            .Complete(jobId, new PlanCompletedData { TotalChangedCount = totalChanged });
    }

    public async Task CompleteApplyFromPlanAsync(Guid jobId, int? actualResourceCount = 1)
    {
        using var _ = CallerCtx.Begin(CallerKind.Runner);
        await new ApplyFromPlanHandler(NullLogger<ApplyFromPlanHandler>.Instance, _bus)
            .Complete(jobId, actualResourceCount);
    }

    public async Task FaultPlanAsync(Guid jobId, string error)
    {
        using var _ = CallerCtx.Begin(CallerKind.Runner);
        await new PlanHandler(NullLogger<PlanHandler>.Instance, _bus)
            .Fault(jobId, error, null);
    }
}
