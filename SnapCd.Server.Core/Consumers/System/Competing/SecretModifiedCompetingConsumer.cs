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
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.Consumers.System.Competing;

public class SecretModifiedCompetingConsumer : IConsumer<SecretModifiedEvent>
{
    private readonly SnapCdDbContext _dbContext;
    private readonly IBus _bus;

    public SecretModifiedCompetingConsumer(SnapCdDbContext dbContext, IBus bus)
    {
        _dbContext = dbContext;
        _bus = bus;
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
            // Modules in namespaces with UseByDefault param references
            from module in _dbContext.Modules
            join np in _dbContext.NamespaceParamFromSecrets on module.NamespaceId equals np.NamespaceId
            where module.TriggerOnDefinitionChanged && np.SecretId == secretId && np.UsageMode == NamespaceInputUsageMode.UseByDefault
            select module.Id
        ).Union(
            // Modules in namespaces with UseByDefault env var references
            from module in _dbContext.Modules
            join ne in _dbContext.NamespaceEnvVarFromSecrets on module.NamespaceId equals ne.NamespaceId
            where module.TriggerOnDefinitionChanged && ne.SecretId == secretId && ne.UsageMode == NamespaceInputUsageMode.UseByDefault
            select module.Id
        ).Union(
            // Modules that select UseIfSelected namespace params
            from module in _dbContext.Modules
            join mpn in _dbContext.ModuleParamFromNamespaces on module.Id equals mpn.ModuleId
            join nps in _dbContext.NamespaceParamFromSecrets on mpn.NamespaceInputId equals nps.Id
            where module.TriggerOnDefinitionChanged && nps.SecretId == secretId && nps.UsageMode == NamespaceInputUsageMode.UseIfSelected
            select module.Id
        ).Union(
            // Modules that select UseIfSelected namespace env vars
            from module in _dbContext.Modules
            join men in _dbContext.ModuleEnvVarFromNamespaces on module.Id equals men.ModuleId
            join nes in _dbContext.NamespaceEnvVarFromSecrets on men.NamespaceInputId equals nes.Id
            where module.TriggerOnDefinitionChanged && nes.SecretId == secretId && nes.UsageMode == NamespaceInputUsageMode.UseIfSelected
            select module.Id
        ).Distinct().ToList();

        // Publish events for modules that need triggering
        foreach (var moduleId in modulesToTrigger)
        {
            Console.WriteLine($"Publishing ModuleModifiedTriggerRequested with ModuleId {moduleId} due to secret change {secretId}");
            await _bus.Publish(new ModuleModifiedTriggerRequested { ModuleId = moduleId, OrganizationId = context.Message.OrganizationId });
        }
    }
}