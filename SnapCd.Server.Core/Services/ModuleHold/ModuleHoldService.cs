// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleHolds;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.ModuleHold;

/// <summary>
/// Holds modules out of deployment while a client (the provider, during an apply) is still configuring them, for as
/// long as the client keeps sending heartbeats for them.
/// </summary>
public class ModuleHoldService
{
    private readonly ModuleHoldStore _store;
    private readonly ModuleSecuredRepositoryFactory _moduleRepositoryFactory;
    private readonly NamespaceSecuredRepositoryFactory _namespaceRepositoryFactory;
    private readonly StackSecuredRepositoryFactory _stackRepositoryFactory;
    private readonly IPrincipalProvider _principalProvider;

    public ModuleHoldService(
        ModuleHoldStore store,
        ModuleSecuredRepositoryFactory moduleRepositoryFactory,
        NamespaceSecuredRepositoryFactory namespaceRepositoryFactory,
        StackSecuredRepositoryFactory stackRepositoryFactory,
        IPrincipalProvider principalProvider)
    {
        _store = store;
        _moduleRepositoryFactory = moduleRepositoryFactory;
        _namespaceRepositoryFactory = namespaceRepositoryFactory;
        _stackRepositoryFactory = stackRepositoryFactory;
        _principalProvider = principalProvider;
    }

    /// <summary>
    /// The heartbeat interval and expiry the server applies to a request: each value, or its default when unset, clamped
    /// to its limits, with a warning when the limits changed a value the client set.
    /// </summary>
    public static ModuleHoldTiming Timing(ModuleHoldSettings settings, int? heartbeatIntervalSeconds, int? heartbeatBufferSeconds)
    {
        var minHeartbeat = Math.Max(settings.MinHeartbeatIntervalSeconds, 1);
        var maxHeartbeat = Math.Max(settings.MaxHeartbeatIntervalSeconds, minHeartbeat);
        var minBuffer = Math.Max(settings.MinHeartbeatBufferSeconds, 1);
        var maxBuffer = Math.Max(settings.MaxHeartbeatBufferSeconds, minBuffer);

        var heartbeat = Math.Clamp(heartbeatIntervalSeconds ?? settings.DefaultHeartbeatIntervalSeconds, minHeartbeat, maxHeartbeat);
        var buffer = Math.Clamp(heartbeatBufferSeconds ?? settings.DefaultHeartbeatBufferSeconds, minBuffer, maxBuffer);

        var changed = new List<string>();
        if (heartbeatIntervalSeconds != null && heartbeat != heartbeatIntervalSeconds)
            changed.Add($"a heartbeat every {heartbeat} seconds instead of {heartbeatIntervalSeconds} "
                        + $"(this server allows {minHeartbeat} to {maxHeartbeat}; hold_modules_heartbeat_interval_seconds in the Snap CD provider, "
                        + "ModuleHold:MinHeartbeatIntervalSeconds and ModuleHold:MaxHeartbeatIntervalSeconds on the server)");
        if (heartbeatBufferSeconds != null && buffer != heartbeatBufferSeconds)
            changed.Add($"a buffer of {buffer} seconds instead of {heartbeatBufferSeconds} "
                        + $"(this server allows {minBuffer} to {maxBuffer}; hold_modules_heartbeat_buffer_seconds in the Snap CD provider, "
                        + "ModuleHold:MinHeartbeatBufferSeconds and ModuleHold:MaxHeartbeatBufferSeconds on the server)");

        var warning = changed.Count == 0
            ? null
            : $"Modules are held with {string.Join(" and ", changed)}. Set the provider values within these limits to remove this warning.";

        return new ModuleHoldTiming(heartbeat, heartbeat + buffer, warning);
    }

    /// <summary>
    /// Holds each listed module that is not yet held, provided the caller may update it, and keeps holding the ones the
    /// caller already holds. A module held by another principal is left alone.
    /// </summary>
    public async Task<ModuleHoldHeartbeatResponseDto> Heartbeat(Guid organizationId, ModuleHoldHeartbeatRequestDto request)
    {
        var principalId = _principalProvider.GetSubject(organizationId);
        var timing = Timing(_store.Settings, request.HeartbeatIntervalSeconds, request.HeartbeatBufferSeconds);

        using var modules = _moduleRepositoryFactory.Create(_principalProvider);
        using var namespaces = _namespaceRepositoryFactory.Create(_principalProvider);
        using var stacks = _stackRepositoryFactory.Create(_principalProvider);

        return new ModuleHoldHeartbeatResponseDto
        {
            EffectiveHeartbeatIntervalSeconds = timing.HeartbeatIntervalSeconds,
            EffectiveExpirySeconds = timing.ExpirySeconds,
            Warning = timing.Warning,
            NotHeldModuleIds = await HoldEach(request.ModuleIds, id => ModuleHoldKey.Module(organizationId, id),
                id => modules.CanUpdate(id, organizationId), principalId, timing.ExpirySeconds),
            NotHeldNamespaceIds = await HoldEach(request.NamespaceIds, id => ModuleHoldKey.Namespace(organizationId, id),
                id => namespaces.CanUpdate(id, organizationId), principalId, timing.ExpirySeconds),
            NotHeldStackIds = await HoldEach(request.StackIds, id => ModuleHoldKey.Stack(organizationId, id),
                id => stacks.CanUpdate(id, organizationId), principalId, timing.ExpirySeconds)
        };
    }

    /// <summary>Holds each id not yet held that the caller may update, refreshes the caller's own, and returns the rest.</summary>
    private async Task<List<Guid>> HoldEach(List<Guid> ids, Func<Guid, ModuleHoldKey> key, Func<Guid, bool> canUpdate, Guid principalId, int expirySeconds)
    {
        var notHeld = new List<Guid>();
        foreach (var id in ids.Distinct())
        {
            var existing = await _store.Get(key(id));
            var mayHold = existing != null && _store.Now < existing.ExpiresAt
                ? existing.PrincipalId == principalId
                : canUpdate(id);

            if (mayHold)
                await _store.Hold(key(id), principalId, expirySeconds);
            else
                notHeld.Add(id);
        }

        return notHeld;
    }

    /// <summary>Holds a module that is about to be created; the caller's right to create it is checked by the create itself.</summary>
    public Task HoldNewModule(Guid organizationId, Guid moduleId, int? heartbeatIntervalSeconds, int? heartbeatBufferSeconds) =>
        _store.Hold(ModuleHoldKey.Module(organizationId, moduleId), _principalProvider.GetSubject(organizationId),
            Timing(_store.Settings, heartbeatIntervalSeconds, heartbeatBufferSeconds).ExpirySeconds);

    /// <summary>Releases the listed holds the caller owns.</summary>
    public async Task Release(Guid organizationId, ModuleHoldReleaseRequestDto request)
    {
        var principalId = _principalProvider.GetSubject(organizationId);
        var keys = request.ModuleIds.Distinct().Select(id => ModuleHoldKey.Module(organizationId, id))
            .Concat(request.NamespaceIds.Distinct().Select(id => ModuleHoldKey.Namespace(organizationId, id)))
            .Concat(request.StackIds.Distinct().Select(id => ModuleHoldKey.Stack(organizationId, id)));

        foreach (var key in keys)
        {
            var existing = await _store.Get(key);
            if (existing?.PrincipalId == principalId)
                await _store.Remove(key);
        }
    }
}

/// <summary>The heartbeat interval and expiry applied to a hold, and why they differ from the request, if they do.</summary>
public record ModuleHoldTiming(int HeartbeatIntervalSeconds, int ExpirySeconds, string? Warning);
