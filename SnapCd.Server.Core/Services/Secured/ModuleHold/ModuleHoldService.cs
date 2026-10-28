// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleHolds;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Interfaces.PrincipalProvider;
using SnapCd.Server.Core.Services.Nonsecured.ModuleHold;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services.Secured.ModuleHold;

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
    /// Holds each listed module, namespace and stack the caller may update until at least one expiry from now. Holds are
    /// not owned, so parallel callers keep one hold alive together and it lapses after the last of their heartbeats.
    /// </summary>
    public async Task<ModuleHoldHeartbeatResponseDto> Heartbeat(Guid organizationId, ModuleHoldHeartbeatRequestDto request)
    {
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
                id => modules.CanUpdate(id, organizationId), timing.ExpirySeconds),
            NotHeldNamespaceIds = await HoldEach(request.NamespaceIds, id => ModuleHoldKey.Namespace(organizationId, id),
                id => namespaces.CanUpdate(id, organizationId), timing.ExpirySeconds),
            NotHeldStackIds = await HoldEach(request.StackIds, id => ModuleHoldKey.Stack(organizationId, id),
                id => stacks.CanUpdate(id, organizationId), timing.ExpirySeconds)
        };
    }

    /// <summary>Holds each id the caller may update and returns the rest.</summary>
    private async Task<List<Guid>> HoldEach(List<Guid> ids, Func<Guid, ModuleHoldKey> key, Func<Guid, bool> canUpdate, int expirySeconds)
    {
        var notHeld = new List<Guid>();
        foreach (var id in ids.Distinct())
        {
            if (canUpdate(id))
                await _store.Hold(key(id), expirySeconds);
            else
                notHeld.Add(id);
        }

        return notHeld;
    }

    /// <summary>Holds a module that is about to be created; the caller's right to create it is checked by the create itself.</summary>
    public Task HoldNewModule(Guid organizationId, Guid moduleId, int? heartbeatIntervalSeconds, int? heartbeatBufferSeconds) =>
        _store.Hold(ModuleHoldKey.Module(organizationId, moduleId),
            Timing(_store.Settings, heartbeatIntervalSeconds, heartbeatBufferSeconds).ExpirySeconds);
}

/// <summary>The heartbeat interval and expiry applied to a hold, and why they differ from the request, if they do.</summary>
public record ModuleHoldTiming(int HeartbeatIntervalSeconds, int ExpirySeconds, string? Warning);
