// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Integrations;
using SnapCd.Server.Core.Entities.Definition.IntegrationEvents;

namespace SnapCd.Server.Core.Mappers;

public static class IntegrationEventMapper
{
    public static IntegrationEventDto ToDto(OrganizationIntegrationEvent e)
        => new() { Id = e.Id, Scope = IntegrationEventScope.Organization, ScopeId = null, IntegrationId = e.IntegrationId, Trigger = e.Trigger, Template = e.Template, Filter = e.Filter, IsDisabled = e.IsDisabled };

    public static IntegrationEventDto ToDto(StackIntegrationEvent e)
        => new() { Id = e.Id, Scope = IntegrationEventScope.Stack, ScopeId = e.StackId, IntegrationId = e.IntegrationId, Trigger = e.Trigger, Template = e.Template, Filter = e.Filter, IsDisabled = e.IsDisabled };

    public static IntegrationEventDto ToDto(NamespaceIntegrationEvent e)
        => new() { Id = e.Id, Scope = IntegrationEventScope.Namespace, ScopeId = e.NamespaceId, IntegrationId = e.IntegrationId, Trigger = e.Trigger, Template = e.Template, Filter = e.Filter, IsDisabled = e.IsDisabled };

    public static IntegrationEventDto ToDto(ModuleIntegrationEvent e)
        => new() { Id = e.Id, Scope = IntegrationEventScope.Module, ScopeId = e.ModuleId, IntegrationId = e.IntegrationId, Trigger = e.Trigger, Template = e.Template, Filter = e.Filter, IsDisabled = e.IsDisabled };
}
