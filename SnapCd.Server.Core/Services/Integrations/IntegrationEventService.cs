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
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;

namespace SnapCd.Server.Core.Services.Integrations;

/// <summary>
/// CRUD for integration-event subscriptions (the demand side). Managing an event requires update permission
/// on the *target integration* (you're configuring how that integration behaves) — consistent with how
/// supply assignments are gated.
/// </summary>
public sealed class IntegrationEventService(
    OrganizationIntegrationEventRepositoryFactory orgFactory,
    StackIntegrationEventRepositoryFactory stackFactory,
    NamespaceIntegrationEventRepositoryFactory namespaceFactory,
    ModuleIntegrationEventRepositoryFactory moduleFactory,
    IntegrationSecuredRepositoryFactory securedFactory)
{
    public async Task<List<IntegrationEventDto>> List(Guid organizationId)
    {
        using var orgRepo = orgFactory.Create();
        var org = (await orgRepo.List(organizationId)).Select(Mappers.IntegrationEventMapper.ToDto);
        using var stackRepo = stackFactory.Create();
        var stack = (await stackRepo.List(organizationId)).Select(Mappers.IntegrationEventMapper.ToDto);
        using var nsRepo = namespaceFactory.Create();
        var ns = (await nsRepo.List(organizationId)).Select(Mappers.IntegrationEventMapper.ToDto);
        using var modRepo = moduleFactory.Create();
        var mod = (await modRepo.List(organizationId)).Select(Mappers.IntegrationEventMapper.ToDto);
        return [.. org, .. stack, .. ns, .. mod];
    }

    public async Task<IntegrationEventDto> GetOne(Guid organizationId, IntegrationEventScope scope, Guid id)
    {
        switch (scope)
        {
            case IntegrationEventScope.Organization:
                using (var repo = orgFactory.Create()) return Mappers.IntegrationEventMapper.ToDto(await repo.Get(id, organizationId));
            case IntegrationEventScope.Stack:
                using (var repo = stackFactory.Create()) return Mappers.IntegrationEventMapper.ToDto(await repo.Get(id, organizationId));
            case IntegrationEventScope.Namespace:
                using (var repo = namespaceFactory.Create()) return Mappers.IntegrationEventMapper.ToDto(await repo.Get(id, organizationId));
            case IntegrationEventScope.Module:
                using (var repo = moduleFactory.Create()) return Mappers.IntegrationEventMapper.ToDto(await repo.Get(id, organizationId));
            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown event scope.");
        }
    }

    public async Task<Guid> Create(Guid organizationId, IntegrationEventCreateDto dto)
    {
        EnsureCanManage(dto.IntegrationId, organizationId);

        var id = Guid.NewGuid();
        switch (dto.Scope)
        {
            case IntegrationEventScope.Organization:
                using (var repo = orgFactory.Create())
                    await repo.Create(new OrganizationIntegrationEvent { Id = id, OrganizationId = organizationId, IntegrationId = dto.IntegrationId, Trigger = dto.Trigger, Template = dto.Template, Filter = dto.Filter, IsDisabled = dto.IsDisabled });
                break;
            case IntegrationEventScope.Stack:
                using (var repo = stackFactory.Create())
                    await repo.Create(new StackIntegrationEvent { Id = id, OrganizationId = organizationId, StackId = RequireScopeId(dto.ScopeId), IntegrationId = dto.IntegrationId, Trigger = dto.Trigger, Template = dto.Template, Filter = dto.Filter, IsDisabled = dto.IsDisabled });
                break;
            case IntegrationEventScope.Namespace:
                using (var repo = namespaceFactory.Create())
                    await repo.Create(new NamespaceIntegrationEvent { Id = id, OrganizationId = organizationId, NamespaceId = RequireScopeId(dto.ScopeId), IntegrationId = dto.IntegrationId, Trigger = dto.Trigger, Template = dto.Template, Filter = dto.Filter, IsDisabled = dto.IsDisabled });
                break;
            case IntegrationEventScope.Module:
                using (var repo = moduleFactory.Create())
                    await repo.Create(new ModuleIntegrationEvent { Id = id, OrganizationId = organizationId, ModuleId = RequireScopeId(dto.ScopeId), IntegrationId = dto.IntegrationId, Trigger = dto.Trigger, Template = dto.Template, Filter = dto.Filter, IsDisabled = dto.IsDisabled });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dto), dto.Scope, "Unknown event scope.");
        }
        return id;
    }

    public async Task Update(Guid organizationId, IntegrationEventScope scope, Guid id, IntegrationEventUpdateDto dto)
    {
        EnsureCanManage(dto.IntegrationId, organizationId);

        switch (scope)
        {
            case IntegrationEventScope.Organization:
                using (var repo = orgFactory.Create())
                {
                    var e = await repo.Get(id, organizationId);
                    e.IntegrationId = dto.IntegrationId; e.Trigger = dto.Trigger; e.Template = dto.Template; e.Filter = dto.Filter; e.IsDisabled = dto.IsDisabled;
                    await repo.Update(e);
                }
                break;
            case IntegrationEventScope.Stack:
                using (var repo = stackFactory.Create())
                {
                    var e = await repo.Get(id, organizationId);
                    e.IntegrationId = dto.IntegrationId; e.Trigger = dto.Trigger; e.Template = dto.Template; e.Filter = dto.Filter; e.IsDisabled = dto.IsDisabled;
                    await repo.Update(e);
                }
                break;
            case IntegrationEventScope.Namespace:
                using (var repo = namespaceFactory.Create())
                {
                    var e = await repo.Get(id, organizationId);
                    e.IntegrationId = dto.IntegrationId; e.Trigger = dto.Trigger; e.Template = dto.Template; e.Filter = dto.Filter; e.IsDisabled = dto.IsDisabled;
                    await repo.Update(e);
                }
                break;
            case IntegrationEventScope.Module:
                using (var repo = moduleFactory.Create())
                {
                    var e = await repo.Get(id, organizationId);
                    e.IntegrationId = dto.IntegrationId; e.Trigger = dto.Trigger; e.Template = dto.Template; e.Filter = dto.Filter; e.IsDisabled = dto.IsDisabled;
                    await repo.Update(e);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown event scope.");
        }
    }

    public async Task Delete(Guid organizationId, IntegrationEventScope scope, Guid id)
    {
        var dto = await GetOne(organizationId, scope, id);
        EnsureCanManage(dto.IntegrationId, organizationId);

        switch (scope)
        {
            case IntegrationEventScope.Organization: using (var repo = orgFactory.Create()) await repo.Delete(id, organizationId); break;
            case IntegrationEventScope.Stack: using (var repo = stackFactory.Create()) await repo.Delete(id, organizationId); break;
            case IntegrationEventScope.Namespace: using (var repo = namespaceFactory.Create()) await repo.Delete(id, organizationId); break;
            case IntegrationEventScope.Module: using (var repo = moduleFactory.Create()) await repo.Delete(id, organizationId); break;
        }
    }

    private void EnsureCanManage(Guid integrationId, Guid organizationId)
    {
        using var secured = securedFactory.Create();
        if (!secured.CanUpdate(integrationId, organizationId))
            throw new PrincipalNotAuthorizedException($"Not permitted to manage events for integration '{integrationId}'.");
    }

    private static Guid RequireScopeId(Guid? scopeId)
        => scopeId ?? throw new ArgumentException("ScopeId is required for a non-organization scope.");
}
