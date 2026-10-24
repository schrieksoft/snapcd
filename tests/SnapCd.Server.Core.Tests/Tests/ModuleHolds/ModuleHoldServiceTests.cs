// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.ModuleHolds;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.ModuleHold;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.ModuleHolds;

[Collection("NewRoleBasedSharedFixture")]
public class ModuleHoldServiceTests
{
    private readonly Fixture _fixture;
    private readonly ManualClock _clock = new(new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero));
    private readonly IDistributedCache _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    private readonly ModuleHoldSettings _settings = new();
    private readonly Guid _organizationId;
    private readonly Guid _moduleId;

    public ModuleHoldServiceTests(Fixture fixture)
    {
        _fixture = fixture;
        _organizationId = fixture.Organizations["0"].Id;
        _moduleId = fixture.Modules["0000"].Id;
    }

    private sealed class ManualClock(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }

    private sealed class DbFactory(Fixture fixture) : IDbContextFactory<SnapCdDbContext>
    {
        public SnapCdDbContext CreateDbContext() => fixture.CreateDbContext();
    }

    private ModuleHoldService Service(OrganizationRole role)
    {
        var userId = _fixture.OrganizationPrincipals["0"][role].DirectUser.Id;
        var principal = _fixture.CreatePrincipalProvider(userId, PrincipalDiscriminator.User, _organizationId);
        var db = new DbFactory(_fixture);
        var bus = Mock.Of<IPublishEndpoint>();
        return new ModuleHoldService(
            Store,
            new ModuleSecuredRepositoryFactory(db, bus, Options.Create(new ModuleRepositorySettings())),
            new NamespaceSecuredRepositoryFactory(db, bus, Options.Create(new NamespaceRepositorySettings())),
            new StackSecuredRepositoryFactory(db, bus, Options.Create(new StackRepositorySettings())),
            principal);
    }

    private ModuleHoldStore Store => new(_cache, Options.Create(_settings), _clock, new DbFactory(_fixture));

    private ModuleHoldHeartbeatRequestDto NamespaceHeartbeat(params Guid[] namespaces) =>
        new() { NamespaceIds = [.. namespaces], HeartbeatIntervalSeconds = 10 };

    private ModuleHoldHeartbeatRequestDto StackHeartbeat(params Guid[] stacks) =>
        new() { StackIds = [.. stacks], HeartbeatIntervalSeconds = 10 };

    private ModuleHoldHeartbeatRequestDto Heartbeat(int heartbeat = 10, int? buffer = null, params Guid[] modules) =>
        new() { ModuleIds = modules.Length == 0 ? [_moduleId] : [.. modules], HeartbeatIntervalSeconds = heartbeat, HeartbeatBufferSeconds = buffer };

    private async Task<bool> Held(Guid? moduleId = null) => (await Store.GetHoldState(_organizationId, moduleId ?? _moduleId)).Held;

    [Theory]
    [InlineData(null, null, 10, 20)]
    [InlineData(10, 5, 10, 15)]
    [InlineData(20, null, 20, 30)]
    [InlineData(null, 30, 10, 40)]
    public void Values_within_the_limits_and_unset_values_are_applied_without_a_warning(int? heartbeat, int? buffer, int expectedHeartbeat, int expectedExpiry)
    {
        var timing = ModuleHoldService.Timing(new ModuleHoldSettings(), heartbeat, buffer);
        Assert.Equal((expectedHeartbeat, expectedExpiry), (timing.HeartbeatIntervalSeconds, timing.ExpirySeconds));
        Assert.Null(timing.Warning);
    }

    [Theory]
    [InlineData(60, null, 30, 40, "hold_modules_heartbeat_interval_seconds")]
    [InlineData(0, null, 5, 15, "hold_modules_heartbeat_interval_seconds")]
    [InlineData(10, 60, 10, 40, "hold_modules_heartbeat_buffer_seconds")]
    [InlineData(10, 0, 10, 11, "hold_modules_heartbeat_buffer_seconds")]
    public void A_value_outside_its_limits_is_clamped_with_a_warning_naming_the_setting(int? heartbeat, int? buffer, int expectedHeartbeat, int expectedExpiry, string setting)
    {
        var timing = ModuleHoldService.Timing(new ModuleHoldSettings(), heartbeat, buffer);
        Assert.Equal((expectedHeartbeat, expectedExpiry), (timing.HeartbeatIntervalSeconds, timing.ExpirySeconds));
        Assert.NotNull(timing.Warning);
        Assert.Contains(setting, timing.Warning);
    }

    [Fact]
    public void Both_values_outside_their_limits_are_named_in_one_warning()
    {
        var timing = ModuleHoldService.Timing(new ModuleHoldSettings(), 60, 60);
        Assert.Equal((30, 60), (timing.HeartbeatIntervalSeconds, timing.ExpirySeconds));
        Assert.Contains("hold_modules_heartbeat_interval_seconds", timing.Warning);
        Assert.Contains("hold_modules_heartbeat_buffer_seconds", timing.Warning);
    }

    [Fact]
    public void A_default_outside_the_limits_is_clamped_without_a_warning()
    {
        var settings = new ModuleHoldSettings { DefaultHeartbeatIntervalSeconds = 100, DefaultHeartbeatBufferSeconds = 0 };
        var timing = ModuleHoldService.Timing(settings, null, null);
        Assert.Equal((30, 31), (timing.HeartbeatIntervalSeconds, timing.ExpirySeconds));
        Assert.Null(timing.Warning);
    }

    [Fact]
    public void The_buffer_is_at_least_one_second_whatever_the_limits()
    {
        var settings = new ModuleHoldSettings { MinHeartbeatBufferSeconds = 0 };
        Assert.Equal(11, ModuleHoldService.Timing(settings, 10, 0).ExpirySeconds);
    }

    [Fact]
    public async Task A_heartbeat_holds_a_module_until_its_expiry()
    {
        var response = await Service(OrganizationRole.Owner).Heartbeat(_organizationId, Heartbeat());

        Assert.Equal(20, response.EffectiveExpirySeconds);
        Assert.Empty(response.NotHeldModuleIds);
        Assert.True(await Held());
        _clock.Advance(TimeSpan.FromSeconds(19));
        Assert.True(await Held());
        _clock.Advance(TimeSpan.FromSeconds(1));
        Assert.False(await Held());
    }

    [Fact]
    public async Task Each_heartbeat_moves_the_expiry_on()
    {
        var service = Service(OrganizationRole.Owner);
        await service.Heartbeat(_organizationId, Heartbeat());

        _clock.Advance(TimeSpan.FromSeconds(15));
        await service.Heartbeat(_organizationId, Heartbeat());
        _clock.Advance(TimeSpan.FromSeconds(15));

        var state = await Store.GetHoldState(_organizationId, _moduleId);
        Assert.True(state.Held);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime.AddSeconds(5), state.ExpiresAt);
    }

    [Fact]
    public async Task A_hold_lasts_as_long_as_heartbeats_keep_coming()
    {
        var service = Service(OrganizationRole.Owner);
        for (var i = 0; i < 360; i++)
        {
            await service.Heartbeat(_organizationId, Heartbeat());
            _clock.Advance(TimeSpan.FromSeconds(10));
        }

        Assert.True(await Held());
    }

    [Fact]
    public async Task A_hold_lost_from_the_cache_is_recreated_by_the_next_heartbeat()
    {
        var service = Service(OrganizationRole.Owner);
        await service.Heartbeat(_organizationId, Heartbeat());
        await Store.Remove(ModuleHoldKey.Module(_organizationId, _moduleId));

        await service.Heartbeat(_organizationId, Heartbeat());

        Assert.True(await Held());
    }

    [Fact]
    public async Task Release_ends_the_hold_at_once()
    {
        var service = Service(OrganizationRole.Owner);
        await service.Heartbeat(_organizationId, Heartbeat());

        await service.Release(_organizationId, new ModuleHoldReleaseRequestDto { ModuleIds = [_moduleId] });

        Assert.False(await Held());
    }

    [Fact]
    public async Task A_module_without_a_hold_is_not_held() =>
        Assert.False(await Held(Guid.NewGuid()));

    [Fact]
    public async Task A_caller_who_cannot_update_the_module_cannot_hold_it()
    {
        var response = await Service(OrganizationRole.Reader).Heartbeat(_organizationId, Heartbeat());

        Assert.Equal([_moduleId], response.NotHeldModuleIds);
        Assert.False(await Held());
    }

    [Fact]
    public async Task Another_principals_hold_is_neither_refreshed_nor_released()
    {
        await Service(OrganizationRole.Owner).Heartbeat(_organizationId, Heartbeat());
        var before = (await Store.GetHoldState(_organizationId, _moduleId)).ExpiresAt;
        var other = Service(OrganizationRole.Contributor);

        _clock.Advance(TimeSpan.FromSeconds(5));
        var response = await other.Heartbeat(_organizationId, Heartbeat());
        await other.Release(_organizationId, new ModuleHoldReleaseRequestDto { ModuleIds = [_moduleId] });

        Assert.Equal([_moduleId], response.NotHeldModuleIds);
        Assert.Equal(before, (await Store.GetHoldState(_organizationId, _moduleId)).ExpiresAt);
    }

    [Fact]
    public async Task A_new_module_is_held_for_its_heartbeat_plus_the_buffer()
    {
        var newModuleId = Guid.NewGuid();

        await Service(OrganizationRole.Owner).HoldNewModule(_organizationId, newModuleId, 10, 5);

        var state = await Store.GetHoldState(_organizationId, newModuleId);
        Assert.True(state.Held);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime.AddSeconds(15), state.ExpiresAt);
    }

    [Fact]
    public async Task A_namespace_hold_holds_every_module_in_the_namespace_and_no_other()
    {
        await Service(OrganizationRole.Owner).Heartbeat(_organizationId, NamespaceHeartbeat(_fixture.Namespaces["000"].Id));

        Assert.True(await Held(_fixture.Modules["0000"].Id));
        Assert.True(await Held(_fixture.Modules["0001"].Id));
        Assert.False(await Held(_fixture.Modules["0010"].Id));
    }

    [Fact]
    public async Task A_stack_hold_holds_every_module_in_its_namespaces_and_no_other()
    {
        await Service(OrganizationRole.Owner).Heartbeat(_organizationId, StackHeartbeat(_fixture.Stacks["00"].Id));

        Assert.True(await Held(_fixture.Modules["0000"].Id));
        Assert.True(await Held(_fixture.Modules["0010"].Id));
        Assert.False(await Held(_fixture.Modules["0100"].Id));
    }

    [Fact]
    public async Task A_namespace_or_stack_hold_lapses_at_its_expiry()
    {
        await Service(OrganizationRole.Owner).Heartbeat(_organizationId, NamespaceHeartbeat(_fixture.Namespaces["000"].Id));
        await Service(OrganizationRole.Owner).Heartbeat(_organizationId, StackHeartbeat(_fixture.Stacks["01"].Id));

        _clock.Advance(TimeSpan.FromSeconds(21));

        Assert.False(await Held(_fixture.Modules["0000"].Id));
        Assert.False(await Held(_fixture.Modules["0100"].Id));
    }

    [Fact]
    public async Task A_module_is_held_until_the_latest_of_its_own_namespace_and_stack_holds()
    {
        var service = Service(OrganizationRole.Owner);
        await service.Heartbeat(_organizationId, Heartbeat(10, 5));
        _clock.Advance(TimeSpan.FromSeconds(3));
        await service.Heartbeat(_organizationId, NamespaceHeartbeat(_fixture.Namespaces["000"].Id));

        var state = await Store.GetHoldState(_organizationId, _moduleId);

        Assert.Equal(_clock.GetUtcNow().UtcDateTime.AddSeconds(20), state.ExpiresAt);
    }

    [Fact]
    public async Task A_caller_who_cannot_update_the_namespace_or_stack_cannot_hold_it()
    {
        var response = await Service(OrganizationRole.Reader).Heartbeat(_organizationId, new ModuleHoldHeartbeatRequestDto
        {
            NamespaceIds = [_fixture.Namespaces["000"].Id],
            StackIds = [_fixture.Stacks["00"].Id]
        });

        Assert.Equal([_fixture.Namespaces["000"].Id], response.NotHeldNamespaceIds);
        Assert.Equal([_fixture.Stacks["00"].Id], response.NotHeldStackIds);
        Assert.False(await Held());
    }

    [Fact]
    public async Task Release_ends_a_namespace_and_a_stack_hold_at_once()
    {
        var service = Service(OrganizationRole.Owner);
        await service.Heartbeat(_organizationId, NamespaceHeartbeat(_fixture.Namespaces["000"].Id));
        await service.Heartbeat(_organizationId, StackHeartbeat(_fixture.Stacks["00"].Id));

        await service.Release(_organizationId, new ModuleHoldReleaseRequestDto
        {
            NamespaceIds = [_fixture.Namespaces["000"].Id],
            StackIds = [_fixture.Stacks["00"].Id]
        });

        Assert.False(await Held());
    }
}
