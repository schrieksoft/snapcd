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
    private readonly List<Guid> _seededJobs = [];

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
        await db.ManualModuleJobs.Where(j => _seededJobs.Contains(j.Id)).ExecuteDeleteAsync();
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
    public async Task Creating_A_Transfer_Asks_The_Receiver_And_Not_The_Source()
    {
        var receiverId = _fixture.Modules["0001"].Id;
        using var service = Service(Contributor);

        var transfer = await service.Create(_moduleId, _organizationId, receiverId, "main");
        _seeded.Add(transfer.Id);

        Assert.Equal(_moduleId, transfer.SourceModuleId);
        Assert.Equal(receiverId, transfer.ReceiverModuleId);
    }

    [Fact]
    public async Task A_Transfer_Cannot_Name_Its_Source_As_A_Receiver()
    {
        using var service = Service(Contributor);

        await Assert.ThrowsAsync<ManualJobNotAllowedException>(
            () => service.Create(_moduleId, _organizationId, _moduleId, null));
    }

    [Fact]
    public async Task A_Reader_Cannot_Start_A_Transfer()
    {
        var receiverId = _fixture.Modules["0001"].Id;
        using var service = Service(Reader);

        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(
            () => service.Create(_moduleId, _organizationId, receiverId, null));
    }

    [Fact]
    public async Task The_Receiver_Is_Always_Asked_Even_By_An_Initiator_Who_Could_Answer_For_It()
    {
        var receiverId = _fixture.Modules["0001"].Id;
        var published = new List<object>();
        using var service = Service(Contributor, published);

        var transfer = await service.Create(_moduleId, _organizationId, receiverId, "main");
        _seeded.Add(transfer.Id);

        await service.AskReceiverFor(transfer.Id, _organizationId);

        Assert.Equal(ConsentStatus.Pending, transfer.ReceiverConsentStatus);
        Assert.Null(transfer.ReceiverConsentPrincipalId);
        Assert.Equal("main", transfer.ReceiverProveRef);
        Assert.Contains(published, m => m is ConsentRequested);
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
            ReceiverConsentStatus = consent,
            ReceiverProveRef = "main"
        });
        await db.SaveChangesAsync();
        return transferId;
    }

    /// <summary>A running job for a transfer: what "a job is running" means now.</summary>
    private async Task SeedRunningJob(Guid transferId)
    {
        var jobId = Guid.NewGuid();
        _seededJobs.Add(jobId);

        await using var db = _fixture.CreateDbContext();
        db.ManualModuleJobs.Add(new ManualModuleJob
        {
            Id = jobId,
            OrganizationId = _organizationId,
            ModuleId = _moduleId,
            TransferId = transferId,
            TimestampStart = DateTimeOffset.UtcNow,
            JobType = ManualJobTypes.TransferMigrate,
            Status = ExecutionStatus.Running
        });
        await db.SaveChangesAsync();
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
