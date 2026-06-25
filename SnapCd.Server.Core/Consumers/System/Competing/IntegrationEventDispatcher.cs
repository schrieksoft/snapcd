// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Missions;
using SnapCd.Server.Core.Licensing.Models;
using SnapCd.Server.Core.Licensing.Services;
using SnapCd.Server.Core.Services.Integrations;
using SnapCd.Server.Core.Services.Integrations.Codecs;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Consumers.System.Competing;

/// <summary>
/// Competing consumer (one server handles each occurrence) that turns saga/mission events into integration
/// deliveries: maps the event to an <see cref="IntegrationTrigger"/>, runs <see cref="IntegrationEventMatcher"/>
/// (supply ∩ demand), renders the template, and sends via the integration's codec. Idempotent per
/// (occurrence, subscription) via the <see cref="IntegrationDelivery"/> dedupe index; milestones for one
/// mission thread under the first message.
/// </summary>
public class IntegrationEventDispatcher :
    IConsumer<ApplyModuleFailed>, IConsumer<DestroyModuleFailed>,
    IConsumer<ApplyModuleCompleted>, IConsumer<DestroyModuleCompleted>,
    IConsumer<ApplyModuleCancelled>, IConsumer<DestroyModuleCancelled>,
    IConsumer<ModuleJobAwaitingApprovalEvent>,
    IConsumer<MissionMilestoneReported>
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbFactory;
    private readonly IntegrationEventMatcher _matcher;
    private readonly IIntegrationCodecRegistry _codecs;
    private readonly IntegrationSecretStore _secrets;
    private readonly IntegrationConnectionCache _connectionCache;
    private readonly ILicenseInfoProvider _licenseInfoProvider;
    private readonly ILogger<IntegrationEventDispatcher> _logger;
    private readonly string _serverHost;

    public IntegrationEventDispatcher(
        IDbContextFactory<SnapCdDbContext> dbFactory,
        IntegrationEventMatcher matcher,
        IIntegrationCodecRegistry codecs,
        IntegrationSecretStore secrets,
        IntegrationConnectionCache connectionCache,
        ILicenseInfoProvider licenseInfoProvider,
        IOptions<ServerSettings> serverSettings,
        ILogger<IntegrationEventDispatcher> logger)
    {
        _dbFactory = dbFactory;
        _matcher = matcher;
        _codecs = codecs;
        _secrets = secrets;
        _connectionCache = connectionCache;
        _licenseInfoProvider = licenseInfoProvider;
        _logger = logger;
        _serverHost = serverSettings.Value.Host.TrimEnd('/');
    }

    public Task Consume(ConsumeContext<ApplyModuleFailed> c) => DeliverJob(c.Message.ModuleId, c.Message.OrganizationId, c.Message.ModuleJobId, IntegrationTrigger.JobFailed, "Apply", c.CancellationToken);
    public Task Consume(ConsumeContext<DestroyModuleFailed> c) => DeliverJob(c.Message.ModuleId, c.Message.OrganizationId, c.Message.ModuleJobId, IntegrationTrigger.JobFailed, "Destroy", c.CancellationToken);
    public Task Consume(ConsumeContext<ApplyModuleCompleted> c) => DeliverJob(c.Message.ModuleId, c.Message.OrganizationId, c.Message.ModuleJobId, IntegrationTrigger.JobSucceeded, "Apply", c.CancellationToken);
    public Task Consume(ConsumeContext<DestroyModuleCompleted> c) => DeliverJob(c.Message.ModuleId, c.Message.OrganizationId, c.Message.ModuleJobId, IntegrationTrigger.JobSucceeded, "Destroy", c.CancellationToken);
    public Task Consume(ConsumeContext<ApplyModuleCancelled> c) => DeliverJob(c.Message.ModuleId, c.Message.OrganizationId, c.Message.ModuleJobId, IntegrationTrigger.JobCancelled, "Apply", c.CancellationToken);
    public Task Consume(ConsumeContext<DestroyModuleCancelled> c) => DeliverJob(c.Message.ModuleId, c.Message.OrganizationId, c.Message.ModuleJobId, IntegrationTrigger.JobCancelled, "Destroy", c.CancellationToken);
    public Task Consume(ConsumeContext<ModuleJobAwaitingApprovalEvent> c) => DeliverJob(c.Message.ModuleId, c.Message.OrganizationId, c.Message.ModuleJobId, IntegrationTrigger.JobAwaitingApproval, null, c.CancellationToken);

    public async Task Consume(ConsumeContext<MissionMilestoneReported> c)
    {
        var msg = c.Message;
        var ct = c.CancellationToken;

        Guid? moduleId;
        await using (var db = await _dbFactory.CreateDbContextAsync(ct))
        {
            moduleId = await db.ModuleJobs
                .Where(j => j.Id == msg.ModuleJobId && j.OrganizationId == msg.OrganizationId)
                .Select(j => (Guid?)j.ModuleId).FirstOrDefaultAsync(ct);
        }
        if (moduleId is null) return;

        var ctx = new Dictionary<string, string?>
        {
            ["missionType"] = msg.MissionType.ToString(),
            ["kind"] = msg.Kind,
            ["message"] = msg.Message
        };
        await Deliver(moduleId.Value, msg.OrganizationId, msg.ModuleJobId, msg.ModuleJobMissionId,
            IntegrationTrigger.MissionMilestoneReported,
            $"milestone:{msg.ModuleJobMissionRunId}:{msg.ReportedAt.Ticks}", ctx, ct);
    }

    private Task DeliverJob(Guid moduleId, Guid organizationId, Guid jobId, IntegrationTrigger trigger, string? jobType, CancellationToken ct)
        => Deliver(moduleId, organizationId, jobId, missionId: null, trigger, $"job:{jobId}:{trigger}",
            new Dictionary<string, string?> { ["jobType"] = jobType }, ct);

    private async Task Deliver(
        Guid moduleId, Guid organizationId, Guid? jobId, Guid? missionId,
        IntegrationTrigger trigger, string dedupeKey, Dictionary<string, string?> ctx, CancellationToken ct)
    {
        var licenseInfo = await _licenseInfoProvider.GetLicenseInfoAsync(organizationId);
        if (!licenseInfo.Includes(Feature.Integrations))
        {
            _logger.LogDebug("Integration dispatch skipped for organization {OrganizationId}: feature not included in license tier", organizationId);
            return;
        }

        var matches = await _matcher.MatchAsync(moduleId, organizationId, trigger, ct);
        if (matches.Count == 0) return;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        ctx["trigger"] = trigger.ToString();
        ctx["organizationId"] = organizationId.ToString();
        ctx["moduleId"] = moduleId.ToString();
        ctx["jobId"] = jobId?.ToString() ?? "";

        var moduleScope = await db.Modules
            .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
            .Select(m => new { m.Name, NamespaceName = m.Namespace.Name, StackName = m.Namespace.Stack.Name })
            .FirstOrDefaultAsync(ct);
        ctx["moduleName"] = moduleScope?.Name ?? "";
        ctx["namespaceName"] = moduleScope?.NamespaceName ?? "";
        ctx["stackName"] = moduleScope?.StackName ?? "";

        if (moduleScope is not null && jobId is not null)
        {
            var tab = trigger == IntegrationTrigger.JobAwaitingApproval ? "approvals" : "logs";
            ctx["jobUrl"] = $"{_serverHost}/Stacks/{Uri.EscapeDataString(moduleScope.StackName)}/{Uri.EscapeDataString(moduleScope.NamespaceName)}/{Uri.EscapeDataString(moduleScope.Name)}?action=Jobs&job={jobId}&tab={tab}";
        }
        else
        {
            ctx["jobUrl"] = "";
        }

        foreach (var match in matches)
        {
            var integration = await db.Integrations
                .Where(i => i.Id == match.IntegrationId && i.OrganizationId == organizationId)
                .Select(i => new { i.IntegrationType, i.Enabled })
                .FirstOrDefaultAsync(ct);
            if (integration is null || !integration.Enabled) continue;

            var delivery = new IntegrationDelivery
            {
                Id = NewId.NextGuid(),
                OrganizationId = organizationId,
                IntegrationId = match.IntegrationId,
                IntegrationEventId = match.EventId,
                Trigger = trigger,
                ModuleJobId = jobId,
                ModuleJobMissionId = missionId,
                DedupeKey = dedupeKey,
                Status = IntegrationDeliveryStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            db.IntegrationDeliveries.Add(delivery);
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
                continue; // already delivered for this (occurrence, subscription)
            }

            // Thread a mission's milestones under its first message.
            string? threadId = null;
            if (missionId is not null)
            {
                threadId = await db.IntegrationDeliveries
                    .Where(d => d.OrganizationId == organizationId && d.IntegrationId == match.IntegrationId
                                && d.ModuleJobMissionId == missionId && d.MessageId != null && d.Id != delivery.Id)
                    .OrderBy(d => d.CreatedAt).Select(d => d.MessageId).FirstOrDefaultAsync(ct);
            }

            var template = string.IsNullOrWhiteSpace(match.Template)
                ? IntegrationTemplateRenderer.DefaultTemplate(trigger)
                : match.Template!;
            var text = IntegrationTemplateRenderer.Render(template, ctx);

            // Read credentials through the connection cache so we don't hit the secret backend on every event.
            var json = await _connectionCache.GetOrCreateAsync(
                match.IntegrationId,
                () => _secrets.ReadAsync(organizationId, match.IntegrationId));
            if (json is null)
            {
                delivery.Status = IntegrationDeliveryStatus.Failed;
                delivery.Error = "Connection secret missing.";
                await db.SaveChangesAsync(ct);
                continue;
            }

            var codec = _codecs.Get(integration.IntegrationType);
            var connection = codec.Deserialize(json);
            var result = await codec.SendAsync(connection, text, threadId, ct);

            delivery.Status = result.Success ? IntegrationDeliveryStatus.Sent : IntegrationDeliveryStatus.Failed;
            delivery.MessageId = result.MessageId;
            delivery.Error = result.Error;
            await db.SaveChangesAsync(ct);

            if (!result.Success)
                _logger.LogWarning("Integration delivery failed: integration {IntegrationId}, trigger {Trigger}: {Error}",
                    match.IntegrationId, trigger, result.Error);
        }
    }
}
