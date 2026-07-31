// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Moq;
using SnapCd.Contracts;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.StateMachine.Gatekeeping;
using SnapCd.Server.Core.Tests.Infrastructure;
using Module = SnapCd.Server.Core.Entities.Definition.Module;
using Namespace = SnapCd.Server.Core.Entities.Definition.Namespace;

namespace SnapCd.Server.Core.Tests.Tests.Services;

[Collection("NewRoleBasedSharedFixture")]
public class SourceChangedServiceTests : IAsyncLifetime
{
    private readonly Fixture _fixture;
    private string _sourceUrl = null!;
    private Namespace _namespace = null!;
    private Runner _runner = null!;
    private Module _filterOnModule = null!;
    private Module _filterOffModule = null!;
    private IPrincipalProvider _ownerPrincipal = null!;

    public SourceChangedServiceTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _sourceUrl = $"https://github.com/test/source-changed-{Guid.NewGuid():N}";

        await using var dbContext = _fixture.CreateDbContext();
        _namespace = dbContext.Namespaces.First();
        _runner = dbContext.Runners.First(r => r.OrganizationId == _namespace.OrganizationId);

        var ownerAssignment = dbContext.UserOrganizationRoleAssignments
            .First(x => x.OrganizationId == _namespace.OrganizationId && x.RoleName == OrganizationRole.Owner);
        _ownerPrincipal = new LiteralPrincipalProvider(ownerAssignment.PrincipalId, PrincipalDiscriminator.User, [_namespace.OrganizationId]);

        _filterOnModule = CreateModule("notify-filter-on", filterEnabled: true, subdirectory: "modules/app-a");
        _filterOnModule.AdditionalTriggerPaths.Add(new ModuleAdditionalTriggerPath
        {
            Id = Guid.NewGuid(),
            OrganizationId = _namespace.OrganizationId,
            ModuleId = _filterOnModule.Id,
            Path = "shared/scripts",
            CreatedDateTime = DateTime.UtcNow
        });
        _filterOffModule = CreateModule("notify-filter-off", filterEnabled: false, subdirectory: "modules/app-b");

        dbContext.Modules.AddRange(_filterOnModule, _filterOffModule);
        await dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await using var dbContext = _fixture.CreateDbContext();
        dbContext.Modules.RemoveRange(dbContext.Modules.Where(m => m.SourceUrl == _sourceUrl));
        await dbContext.SaveChangesAsync();
    }

    private Module CreateModule(string name, bool filterEnabled, string subdirectory)
    {
        var module = new Module
        {
            Id = Guid.NewGuid(),
            Name = $"{name}-{Guid.NewGuid():N}"[..40],
            NamespaceId = _namespace.Id,
            RunnerId = _runner.Id,
            OrganizationId = _namespace.OrganizationId,
            SourceUrl = _sourceUrl,
            SourceRevision = "main",
            SourceSubdirectory = subdirectory,
            TriggerOnSourceChangedNotification = true,
            TriggerPathFilterEnabled = filterEnabled,
            CreatedDateTime = DateTime.UtcNow
        };
        module.ModuleSaga = new ModuleSaga
        {
            CorrelationId = module.Id,
            OrganizationId = module.OrganizationId,
            RowVersion = [],
            CurrentState = nameof(ModuleStateMachine.Gatekeeping)
        };
        module.ModuleModifiedSaga = new ModuleModifiedSaga
        {
            CorrelationId = module.Id,
            OrganizationId = module.OrganizationId,
            RowVersion = [],
            CurrentState = nameof(ModuleModifiedStateMachine.Idle)
        };
        return module;
    }

    [Fact]
    public async Task Notification_Direct_Triggers_Filter_Off_And_Dispatches_Refresh_For_Filter_On()
    {
        var publishedTriggers = new List<GatekeepingJobRequested>();
        var busMock = new Mock<IBus>();
        busMock
            .Setup(b => b.Publish(It.IsAny<GatekeepingJobRequested>(), It.IsAny<IPipe<PublishContext<GatekeepingJobRequested>>>(), It.IsAny<CancellationToken>()))
            .Callback<GatekeepingJobRequested, IPipe<PublishContext<GatekeepingJobRequested>>, CancellationToken>((message, _, _) => publishedTriggers.Add(message))
            .Returns(Task.CompletedTask);

        var dispatched = new List<(string SourceUrl, List<string> WatchedPaths, bool TriggeredByNotification)>();
        var dispatcherMock = new Mock<SourceRefreshDispatcher>(null!, null!, null!);
        dispatcherMock
            .Setup(d => d.DispatchRefresh(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<SourceType>(), It.IsAny<SourceRevisionType>(), It.IsAny<List<string>>(), It.IsAny<bool>()))
            .Callback<Guid, Guid, string, string, SourceType, SourceRevisionType, List<string>, bool>(
                (_, _, sourceUrl, _, _, _, watchedPaths, byNotification) => dispatched.Add((sourceUrl, watchedPaths, byNotification)))
            .ReturnsAsync(true);

        await using var dbContext = _fixture.CreateDbContext();
        var service = new SourceChangedService(busMock.Object, dbContext, _ownerPrincipal, dispatcherMock.Object);

        await service.NotifyChange(new SourceChangedDto
        {
            SourceUrl = _sourceUrl,
            SourceRevision = "main",
            SourceType = SourceType.Git
        }, _namespace.OrganizationId);

        // Filter-off module: the notification is the trigger, exactly as today.
        var directTrigger = Assert.Single(publishedTriggers);
        Assert.Equal(_filterOffModule.Id, directTrigger.ModuleId);

        // Filter-on module: one targeted refresh for its group; the hash decides.
        var refresh = Assert.Single(dispatched);
        Assert.Equal(_sourceUrl, refresh.SourceUrl);
        Assert.True(refresh.TriggeredByNotification);
        Assert.Equal(new[] { "modules/app-a", "shared/scripts" }, refresh.WatchedPaths);
    }
}
