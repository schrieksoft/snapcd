// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Transfers;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Transfers;

/// <summary>
/// Consent is a decision on the Transfer, given by someone who could pause the receiving Module,
/// answered once, and keyed to the map hash it was given against.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class TransferConsentTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private Guid _moduleId;
    private Guid _organizationId;
    private readonly List<Guid> _seeded = [];
    private readonly List<Guid> _seededUsers = [];

    public TransferConsentTests(Fixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _moduleId = _fixture.Modules["0000"].Id;
        _organizationId = _fixture.Organizations["0"].Id;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await using var db = _fixture.CreateDbContext();
        await db.Transfers.Where(t => _seeded.Contains(t.Id)).ExecuteDeleteAsync();
        await db.UserModuleRoleAssignments.Where(a => _seededUsers.Contains(a.UserId)).ExecuteDeleteAsync();
        await db.OrganizationUsers.Where(u => _seededUsers.Contains(u.UserId)).ExecuteDeleteAsync();
        await db.Users.Where(u => _seededUsers.Contains(u.Id)).ExecuteDeleteAsync();
    }

    private Guid Reader => _fixture.OrganizationPrincipals["0"][OrganizationRole.Reader].DirectUser.Id;
    private Guid Contributor => _fixture.OrganizationPrincipals["0"][OrganizationRole.Contributor].DirectUser.Id;

    [Fact]
    public async Task A_Reader_Cannot_Consent()
    {
        var transferId = await Seed(ConsentStatus.Pending);
        using var service = Service(Reader);

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => service.Decide(transferId, _moduleId, _organizationId, granted: true, "main", null));
    }

    [Fact]
    public async Task A_Contributor_Can_Consent_And_The_Ref_Is_Recorded()
    {
        var transferId = await Seed(ConsentStatus.Pending);
        using var service = Service(Contributor);

        await service.Decide(transferId, _moduleId, _organizationId, granted: true, "release/1.2", "looks right");

        var participant = await Participant(transferId);
        Assert.Equal(ConsentStatus.Granted, participant.ReceiverConsentStatus);
        Assert.Equal(Contributor, participant.ReceiverConsentPrincipalId);
        Assert.Equal("release/1.2", participant.ReceiverProveRef);
        Assert.Equal("looks right", participant.ReceiverConsentReason);
        Assert.NotNull(participant.ReceiverConsentDecidedAt);
    }

    [Fact]
    public async Task A_Refusal_Is_Recorded_As_Refused()
    {
        var transferId = await Seed(ConsentStatus.Pending);
        using var service = Service(Contributor);

        await service.Decide(transferId, _moduleId, _organizationId, granted: false, null, "not now");

        Assert.Equal(ConsentStatus.Refused, (await Participant(transferId)).ReceiverConsentStatus);
    }

    /// <summary>A second answer is refused rather than silently replacing the first.</summary>
    [Fact]
    public async Task Consent_Cannot_Be_Given_Twice()
    {
        var transferId = await Seed(ConsentStatus.Granted);
        using var service = Service(Contributor);

        await Assert.ThrowsAsync<ManualJobNotAllowedException>(
            () => service.Decide(transferId, _moduleId, _organizationId, granted: true, "main", null));
    }

    [Fact]
    public async Task The_Prove_Ref_Cannot_Change_After_Locking()
    {
        var transferId = await Seed(ConsentStatus.Granted, lockedAt: DateTimeOffset.UtcNow);
        using var service = Service(Contributor);

        await Assert.ThrowsAsync<ManualJobNotAllowedException>(
            () => service.SetProveRef(transferId, _moduleId, _organizationId, "other"));
    }

    [Fact]
    public async Task Consent_Can_Be_Revoked_While_No_Migrate_Runs()
    {
        var transferId = await Seed(ConsentStatus.Granted);
        using var service = Service(Contributor);

        await service.Revoke(transferId, _moduleId, _organizationId, "changed my mind");

        Assert.Equal(ConsentStatus.Revoked, (await Participant(transferId)).ReceiverConsentStatus);
    }

    [Fact]
    public async Task Consent_Cannot_Be_Revoked_While_A_Migrate_Runs()
    {
        var transferId = await Seed(ConsentStatus.Granted, status: TransferStatus.Migrating);
        using var service = Service(Contributor);

        await Assert.ThrowsAsync<ManualJobNotAllowedException>(
            () => service.Revoke(transferId, _moduleId, _organizationId, null));
    }

    /// <summary>Consent is keyed to a map hash, so a changed map asks again and drops the lock.</summary>
    [Fact]
    public async Task Replacing_The_Map_Returns_Consent_To_Pending_And_Releases_The_Lock()
    {
        var transferId = await Seed(ConsentStatus.Granted, lockedAt: DateTimeOffset.UtcNow, mergedAt: DateTimeOffset.UtcNow);
        using var service = Service(Contributor);

        var released = await service.ReplaceMap(transferId, _organizationId, "{\"v\":2}", "hash-two");

        var participant = await Participant(transferId);
        Assert.Contains(_moduleId, released);
        Assert.Null(participant.ReceiverLockedAt);
        Assert.Null(participant.ReceiverMergedDeclaredAt);

        await using var db = _fixture.CreateDbContext();
        var transfer = await db.Transfers.AsNoTracking().SingleAsync(t => t.Id == transferId);
        Assert.Equal("hash-two", transfer.MapHash);
        Assert.Equal(TransferStatus.Open, transfer.Status);
    }

    [Fact]
    public async Task Replacing_The_Map_With_The_Same_Hash_Changes_Nothing()
    {
        var transferId = await Seed(ConsentStatus.Granted, lockedAt: DateTimeOffset.UtcNow);
        using var service = Service(Contributor);

        var released = await service.ReplaceMap(transferId, _organizationId, "{}", "hash-one");

        Assert.Empty(released);
        Assert.NotNull((await Participant(transferId)).ReceiverLockedAt);
    }

    [Theory]
    [InlineData(ConsentStatus.Pending, false, false, TransferStatus.Open)]
    [InlineData(ConsentStatus.Revoked, false, false, TransferStatus.Open)]
    [InlineData(ConsentStatus.Refused, false, false, TransferStatus.Open)]
    [InlineData(ConsentStatus.Granted, false, false, TransferStatus.Proving)]
    [InlineData(ConsentStatus.Granted, true, false, TransferStatus.Merging)]
    [InlineData(ConsentStatus.Granted, true, true, TransferStatus.ReadyToMigrate)]
    public async Task Status_Follows_The_Gates(
        ConsentStatus consent, bool locked, bool merged, TransferStatus expected)
    {
        var transferId = await Seed(consent,
            lockedAt: locked ? DateTimeOffset.UtcNow : null,
            mergedAt: merged ? DateTimeOffset.UtcNow : null);
        using var service = Service(Contributor);

        Assert.Equal(expected, await service.DeriveStatus(transferId, _organizationId));
    }

    /// <summary>A closed Transfer is not recomputed: its status is the record of how it ended.</summary>
    [Theory]
    [InlineData(TransferStatus.Migrated)]
    [InlineData(TransferStatus.Abandoned)]
    public async Task A_Closed_Transfer_Keeps_Its_Status(TransferStatus status)
    {
        var transferId = await Seed(ConsentStatus.Pending, status: status);
        using var service = Service(Contributor);

        Assert.Equal(status, await service.DeriveStatus(transferId, _organizationId));
    }

    [Fact]
    public async Task Abandoning_Closes_The_Transfer_And_Names_The_Held_Modules()
    {
        var transferId = await Seed(ConsentStatus.Granted, lockedAt: DateTimeOffset.UtcNow);
        using var service = Service(Contributor);

        var withdrawn = await service.Abandon(transferId, _organizationId, "not going ahead");

        Assert.Contains(_moduleId, withdrawn);

        await using var db = _fixture.CreateDbContext();
        var transfer = await db.Transfers.AsNoTracking().SingleAsync(t => t.Id == transferId);
        Assert.Equal(TransferStatus.Abandoned, transfer.Status);
        Assert.Equal("not going ahead", transfer.CloseReason);
        Assert.NotNull(transfer.ClosedAt);

        var participant = await Participant(transferId);
        Assert.Null(participant.ReceiverLockedAt);
        Assert.NotNull(participant.ReceiverReleasedAt);
    }

    [Fact]
    public async Task A_Reader_Cannot_Abandon()
    {
        var transferId = await Seed(ConsentStatus.Granted);
        using var service = Service(Reader);

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => service.Abandon(transferId, _organizationId, "no"));
    }

    [Fact]
    public async Task Withdrawing_A_Participant_Drops_Its_Lock()
    {
        var transferId = await Seed(ConsentStatus.Granted, lockedAt: DateTimeOffset.UtcNow);
        using var service = Service(Contributor);

        await service.WithdrawParticipant(transferId, _moduleId, _organizationId, "revoked after locking");

        var participant = await Participant(transferId);
        Assert.Null(participant.ReceiverLockedAt);
        Assert.NotNull(participant.ReceiverReleasedAt);
    }

    /// <summary>
    /// The hash is the transfer's identity on both sides, so it is pinned against a value computed
    /// outside this codebase: sha256 of the map's bytes, lowercase hex, as demonolith computes it.
    /// </summary>
    [Fact]
    public void The_Map_Hash_Matches_Demonoliths()
    {
        Assert.Equal(
            "5d7283ec8b1389d77a70d0f73253199aa34c1e335d52ae4556cb3c73005dda91",
            TransferService.HashMap("version: 1\nsource: app\n"));
    }

    [Fact]
    public async Task Creating_A_Transfer_Asks_The_Receiver_And_Not_The_Source()
    {
        var receiverId = _fixture.Modules["0001"].Id;
        using var service = Service(Contributor);

        var transfer = await service.Create(_moduleId, _organizationId, "version: 1\n", receiverId, "main");
        _seeded.Add(transfer.Id);

        Assert.Equal(_moduleId, transfer.SourceModuleId);
        Assert.Equal(receiverId, transfer.ReceiverModuleId);
        Assert.Equal(TransferService.HashMap("version: 1\n"), transfer.MapHash);
    }

    [Fact]
    public async Task A_Transfer_Cannot_Name_Its_Source_As_A_Receiver()
    {
        using var service = Service(Contributor);

        await Assert.ThrowsAsync<ManualJobNotAllowedException>(
            () => service.Create(_moduleId, _organizationId, "{}", _moduleId, null));
    }

    [Fact]
    public async Task A_Reader_Cannot_Start_A_Transfer()
    {
        var receiverId = _fixture.Modules["0001"].Id;
        using var service = Service(Reader);

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => service.Create(_moduleId, _organizationId, "{}", receiverId, null));
    }

    [Fact]
    public async Task Locking_Requires_Consent()
    {
        var transferId = await Seed(ConsentStatus.Pending);
        using var service = Service(Contributor);

        await Assert.ThrowsAsync<ManualJobNotAllowedException>(
            () => service.Lock(transferId, _moduleId, _organizationId));
    }

    [Fact]
    public async Task Locking_Records_Who_And_When()
    {
        var transferId = await Seed(ConsentStatus.Granted);
        using var service = Service(Contributor);

        await service.Lock(transferId, _moduleId, _organizationId);

        var participant = await Participant(transferId);
        Assert.NotNull(participant.ReceiverLockedAt);
        Assert.Equal(Contributor, participant.ReceiverLockedBy);
    }

    [Fact]
    public async Task Merged_Cannot_Be_Declared_Before_Locking()
    {
        var transferId = await Seed(ConsentStatus.Granted);
        using var service = Service(Contributor);

        await Assert.ThrowsAsync<ManualJobNotAllowedException>(
            () => service.DeclareMerged(transferId, _moduleId, _organizationId, "abc123"));
    }

    [Fact]
    public async Task Declaring_Merged_Records_The_Commit()
    {
        var transferId = await Seed(ConsentStatus.Granted, lockedAt: DateTimeOffset.UtcNow);
        using var service = Service(Contributor);

        await service.DeclareMerged(transferId, _moduleId, _organizationId, "abc123");

        var participant = await Participant(transferId);
        Assert.Equal("abc123", participant.ReceiverMergedCommit);
        Assert.Equal(Contributor, participant.ReceiverMergedDeclaredBy);
    }

    /// <summary>Auto-consent: the initiator could have clicked the button, so the click is implied.</summary>
    [Fact]
    public async Task A_Receiver_The_Initiator_Can_Consent_For_Is_Granted_Without_Asking()
    {
        var receiverId = _fixture.Modules["0001"].Id;
        var published = new List<object>();
        using var service = Service(Contributor, published);

        var transfer = await service.Create(_moduleId, _organizationId, "{}", receiverId, "main");
        _seeded.Add(transfer.Id);

        Assert.Equal(ConsentStatus.Granted, transfer.ReceiverConsentStatus);
        Assert.Equal(Contributor, transfer.ReceiverConsentPrincipalId);
        Assert.Equal("main", transfer.ReceiverProveRef);
        Assert.DoesNotContain(published, m => m is ConsentRequested);
    }

    [Fact]
    public async Task Locking_Asks_For_The_Hold()
    {
        var transferId = await Seed(ConsentStatus.Granted);
        var published = new List<object>();
        using var service = Service(Contributor, published);

        await service.Lock(transferId, _moduleId, _organizationId);

        var hold = Assert.Single(published.OfType<ModuleHoldRequested>());
        Assert.Equal(_moduleId, hold.ModuleId);
        Assert.Equal(transferId, hold.TransferId);
    }

    [Fact]
    public async Task Abandoning_Asks_For_Each_Held_Module_To_Be_Paused()
    {
        var transferId = await Seed(ConsentStatus.Granted, lockedAt: DateTimeOffset.UtcNow);
        var published = new List<object>();
        using var service = Service(Contributor, published);

        await service.Abandon(transferId, _organizationId, "not going ahead");

        // Both sides were held, so both are withdrawn.
        var withdrawn = published.OfType<ModuleWithdrawnFromTransfer>().ToList();
        Assert.Equal(2, withdrawn.Count);
        Assert.Contains(withdrawn, w => w.ModuleId == _moduleId);
        Assert.All(withdrawn, w => Assert.Equal("not going ahead", w.Reason));
    }

    [Fact]
    public async Task A_Map_Edit_Pauses_A_Receiver_That_Had_Merged()
    {
        var transferId = await Seed(ConsentStatus.Granted, lockedAt: DateTimeOffset.UtcNow, mergedAt: DateTimeOffset.UtcNow);
        var published = new List<object>();
        using var service = Service(Contributor, published);

        await service.ReplaceMap(transferId, _organizationId, "v: 2", "hash-two");

        Assert.Single(published.OfType<ModuleWithdrawnFromTransfer>());

        // This editor can consent on the receiver, so the re-ask is auto-granted rather than sent.
        var participant = await Participant(transferId);
        Assert.Equal(ConsentStatus.Granted, participant.ReceiverConsentStatus);
        Assert.Null(participant.ReceiverLockedAt);
    }

    [Fact]
    public async Task Releasing_A_Landed_Participant_Asks_For_A_Real_Release()
    {
        var transferId = await Seed(ConsentStatus.Granted, lockedAt: DateTimeOffset.UtcNow);
        var published = new List<object>();
        using var service = Service(Contributor, published);

        await service.ReleaseLanded(transferId, _moduleId, _organizationId);

        var release = Assert.Single(published.OfType<ModuleReleaseRequested>());
        Assert.Equal(transferId, release.TransferId);
        Assert.DoesNotContain(published, m => m is ModuleWithdrawnFromTransfer);
    }

    [Theory]
    [InlineData("lock")]
    [InlineData("merged")]
    [InlineData("revoke")]
    [InlineData("proveref")]
    public async Task A_Reader_Is_Refused_Every_Gate(string gate)
    {
        var transferId = await Seed(ConsentStatus.Granted, lockedAt: gate == "merged" ? DateTimeOffset.UtcNow : null);
        using var service = Service(Reader);

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(() => gate switch
        {
            "lock" => service.Lock(transferId, _moduleId, _organizationId),
            "merged" => service.DeclareMerged(transferId, _moduleId, _organizationId, "abc"),
            "revoke" => service.Revoke(transferId, _moduleId, _organizationId, null),
            _ => service.SetProveRef(transferId, _moduleId, _organizationId, "other")
        });
    }

    /// <summary>
    /// A consent records who decided in the same shape an approval does: the principal, what kind it
    /// was, and the agent behind it when a ServicePrincipal acted for one.
    /// </summary>
    [Fact]
    public async Task A_Consent_Records_The_Principal_And_Its_Kind()
    {
        var transferId = await Seed(ConsentStatus.Pending);
        using var service = Service(Contributor);

        await service.Decide(transferId, _moduleId, _organizationId, granted: true, "main", null);

        var transfer = await Participant(transferId);
        Assert.Equal(Contributor, transfer.ReceiverConsentPrincipalId);
        Assert.Equal(PrincipalDiscriminator.User, transfer.ReceiverConsentPrincipalDiscriminator);
        Assert.Null(transfer.ReceiverConsentAgentId);
    }

    [Fact]
    public async Task Locking_And_Merging_Record_The_Principal_Kind()
    {
        var transferId = await Seed(ConsentStatus.Granted);
        using var service = Service(Contributor);

        await service.Lock(transferId, _moduleId, _organizationId);
        await service.DeclareMerged(transferId, _moduleId, _organizationId, "abc123");

        var transfer = await Participant(transferId);
        Assert.Equal(PrincipalDiscriminator.User, transfer.ReceiverLockedByPrincipalDiscriminator);
        Assert.Equal(PrincipalDiscriminator.User, transfer.ReceiverMergedDeclaredByPrincipalDiscriminator);
    }

    /// <summary>
    /// Consent is the receiver's to give. Someone who may act on the source but holds nothing on the
    /// receiver is refused, which is the whole point of asking.
    /// </summary>
    [Fact]
    public async Task A_Principal_Without_Rights_On_The_Receiver_Cannot_Consent_For_It()
    {
        var transferId = await Seed(ConsentStatus.Pending);
        var outsider = await SeedModuleContributor(_fixture.Modules["0001"].Id);

        using var service = Service(outsider);

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => service.Decide(transferId, _moduleId, _organizationId, granted: true, "main", null));
    }

    /// <summary>A contributor on one Module only, so rights do not leak across the pair.</summary>
    private async Task<Guid> SeedModuleContributor(Guid moduleId)
    {
        var userId = Guid.NewGuid();
        _seededUsers.Add(userId);

        await using var db = _fixture.CreateDbContext();
        db.Users.Add(new User
        {
            Id = userId,
            UserName = $"scoped-{userId:N}@example.com",
            Email = $"scoped-{userId:N}@example.com",
            IsDisabled = false,
            CreatedDateTime = DateTime.UtcNow
        });
        db.OrganizationUsers.Add(new OrganizationUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            InvitationCompleted = true
        });
        db.UserModuleRoleAssignments.Add(new UserModuleRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = _organizationId,
            ModuleId = moduleId,
            UserId = userId,
            RoleName = ModuleRole.Contributor
        });
        await db.SaveChangesAsync();
        return userId;
    }

    private async Task<Guid> Seed(
        ConsentStatus consent,
        DateTimeOffset? lockedAt = null,
        TransferStatus status = TransferStatus.Open,
        DateTimeOffset? mergedAt = null)
    {
        var transferId = Guid.NewGuid();
        _seeded.Add(transferId);

        await using var db = _fixture.CreateDbContext();
        db.Transfers.Add(new Transfer
        {
            Id = transferId,
            OrganizationId = _organizationId,
            SourceModuleId = _fixture.Modules["0001"].Id,
            ReceiverModuleId = _moduleId,
            MapJson = "{}",
            MapHash = "hash-one",
            Status = status,
            ReceiverConsentStatus = consent,
            ReceiverProveRef = "main",
            ReceiverLockedAt = lockedAt,
            ReceiverMergedDeclaredAt = mergedAt,
            SourceLockedAt = lockedAt,
            SourceMergedDeclaredAt = mergedAt
        });
        await db.SaveChangesAsync();
        return transferId;
    }

    private async Task<Transfer> Participant(Guid transferId)
    {
        await using var db = _fixture.CreateDbContext();
        return await db.Transfers.AsNoTracking().SingleAsync(t => t.Id == transferId);
    }

    /// <summary>A bus that records what the service published, so the events can be asserted.</summary>
    private static IBus RecordingBus(List<object> published)
    {
        var bus = new Mock<IBus>();
        // The service publishes through the generic overload, so each event type is set up by name.
        bus.Setup(x => x.Publish(It.IsAny<ConsentRequested>(), It.IsAny<CancellationToken>()))
            .Callback<ConsentRequested, CancellationToken>((m, _) => published.Add(m))
            .Returns(Task.CompletedTask);
        bus.Setup(x => x.Publish(It.IsAny<ConsentDecided>(), It.IsAny<CancellationToken>()))
            .Callback<ConsentDecided, CancellationToken>((m, _) => published.Add(m))
            .Returns(Task.CompletedTask);
        bus.Setup(x => x.Publish(It.IsAny<ModuleHoldRequested>(), It.IsAny<CancellationToken>()))
            .Callback<ModuleHoldRequested, CancellationToken>((m, _) => published.Add(m))
            .Returns(Task.CompletedTask);
        bus.Setup(x => x.Publish(It.IsAny<ModuleReleaseRequested>(), It.IsAny<CancellationToken>()))
            .Callback<ModuleReleaseRequested, CancellationToken>((m, _) => published.Add(m))
            .Returns(Task.CompletedTask);
        bus.Setup(x => x.Publish(It.IsAny<ModuleWithdrawnFromTransfer>(), It.IsAny<CancellationToken>()))
            .Callback<ModuleWithdrawnFromTransfer, CancellationToken>((m, _) => published.Add(m))
            .Returns(Task.CompletedTask);
        return bus.Object;
    }

    private TransferService Service(Guid principalId, List<object>? published = null)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, PrincipalDiscriminator.User, _organizationId);
        var secured = new ModuleSecuredRepository(
            new ModuleRepository(_fixture.CreateDbContext(), principalProvider, _fixture.CreateMockBus(), _fixture.CreateModuleSettings()),
            principalProvider);
        return new TransferService(DbContextFactory(), secured, principalProvider,
            published != null ? RecordingBus(published) : _fixture.CreateMockBus());
    }

    private IDbContextFactory<SnapCdDbContext> DbContextFactory()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<SnapCdDbContext>(o => o.UseSqlServer(_fixture.ConnectionString));
        return services.BuildServiceProvider().GetRequiredService<IDbContextFactory<SnapCdDbContext>>();
    }
}
