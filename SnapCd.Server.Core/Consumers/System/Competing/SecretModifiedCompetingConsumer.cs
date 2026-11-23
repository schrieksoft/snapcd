// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class SecretModifiedCompetingConsumer : IConsumer<SecretModifiedEvent>
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IBus _bus;
    private readonly ILogger<SecretModifiedCompetingConsumer> _logger;

    public SecretModifiedCompetingConsumer(SnapCdDbContext dbContext, IBus bus, ILogger<SecretModifiedCompetingConsumer> logger)
    {
        _dbContext = dbContext;
        _bus = bus;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SecretModifiedEvent> context)
    {
        var secretId = context.Message.SecretId;

        var modulesToTrigger = (
            // Direct module references via ModuleParamFromSecret
            from module in _dbContext.Modules
            join mp in _dbContext.ModuleParamFromSecrets on module.Id equals mp.ModuleId
            where module.TriggerOnDefinitionChanged && mp.SecretId == secretId
            select module.Id
        ).Union(
            // Direct module references via ModuleEnvVarFromSecret
            from module in _dbContext.Modules
            join me in _dbContext.ModuleEnvVarFromSecrets on module.Id equals me.ModuleId
            where module.TriggerOnDefinitionChanged && me.SecretId == secretId
            select module.Id
        ).Union(
            // A Namespace input reaches every Module in that Namespace and below it.
            from module in _dbContext.Modules
            join above in _dbContext.NamespaceAncestors
                on new { module.NamespaceId, module.OrganizationId } equals new { above.NamespaceId, above.OrganizationId }
            join np in _dbContext.NamespaceParamFromSecrets
                on new { NamespaceId = above.AncestorNamespaceId, above.OrganizationId } equals new { np.NamespaceId, np.OrganizationId }
            where module.TriggerOnDefinitionChanged && np.SecretId == secretId
            select module.Id
        ).Union(
            from module in _dbContext.Modules
            join above in _dbContext.NamespaceAncestors
                on new { module.NamespaceId, module.OrganizationId } equals new { above.NamespaceId, above.OrganizationId }
            join ne in _dbContext.NamespaceEnvVarFromSecrets
                on new { NamespaceId = above.AncestorNamespaceId, above.OrganizationId } equals new { ne.NamespaceId, ne.OrganizationId }
            where module.TriggerOnDefinitionChanged && ne.SecretId == secretId
            select module.Id
        ).Distinct().ToList();

        // Publish events for modules that need triggering
        foreach (var moduleId in modulesToTrigger)
        {
            _logger.LogDebug("Publishing GatekeepingJobRequested for module {ModuleId} due to secret change {SecretId}", moduleId, secretId);
            await _bus.Publish(new GatekeepingJobRequested
            {
                ModuleId = moduleId,
                OrganizationId = context.Message.OrganizationId
            }, publishContext => { publishContext.TimeToLive = TimeSpan.FromMinutes(5); });
        }
    }
}