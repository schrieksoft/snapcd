// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using SnapCd.Contracts;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.Tests.Repositories;

/// <summary>
/// Updates go the way the update service sends them: the entity is loaded through the repository, changed in
/// place and handed back, so anything the repository reads from the tracked entity already has the new values.
/// </summary>
[Collection("NewRoleBasedSharedFixture")]
public class UpdateOriginalValuesTests
{
    private readonly Fixture _fixture;

    public UpdateOriginalValuesTests(Fixture fixture) => _fixture = fixture;

    private static IEnumerable<TEvent> Published<TEvent>(Mock<IPublishEndpoint> bus) =>
        bus.Invocations
            .Where(i => i.Method.Name == nameof(IPublishEndpoint.Publish))
            .Select(i => i.Arguments.FirstOrDefault())
            .OfType<TEvent>();

    private static LiteralPrincipalProvider Principal(Guid organizationId) =>
        new(Guid.Empty, PrincipalDiscriminator.User, [organizationId]);

    private ModuleRepository Modules(SnapCd.Server.Core.Database.SnapCdDbContext db, Guid organizationId, Mock<IPublishEndpoint> bus) =>
        new(db, Principal(organizationId), bus.Object, Options.Create(new ModuleRepositorySettings()));

    [Fact]
    public async Task Changing_a_modules_threshold_publishes_the_re_evaluation_and_reports_the_old_value()
    {
        var module = _fixture.Modules["0000"];
        var bus = new Mock<IPublishEndpoint>();
        await using var db = _fixture.CreateDbContext();
        var repository = Modules(db, module.OrganizationId, bus);
        var original = (await repository.Get(module.Id, module.OrganizationId)).DestroyApprovalThreshold;

        try
        {
            var entity = await repository.Get(module.Id, module.OrganizationId);
            entity.DestroyApprovalThreshold = (original ?? 0) + 1;
            await repository.ExecuteUpdate(entity);

            Assert.Single(Published<ModuleApprovalThresholdModifiedEvent>(bus));
            var updated = Assert.Single(Published<ModuleUpdatedEvent>(bus));
            Assert.Equal(original, updated.PreviousData.DestroyApprovalThreshold);
            Assert.Equal((original ?? 0) + 1, updated.Data.DestroyApprovalThreshold);
        }
        finally
        {
            await db.Modules.Where(m => m.Id == module.Id).ExecuteUpdateAsync(s => s.SetProperty(m => m.DestroyApprovalThreshold, original));
        }
    }

    [Fact]
    public async Task Changing_a_namespaces_default_threshold_publishes_the_re_evaluation()
    {
        var ns = _fixture.Namespaces["000"];
        var bus = new Mock<IPublishEndpoint>();
        await using var db = _fixture.CreateDbContext();
        var repository = new NamespaceRepository(db, Principal(ns.OrganizationId), bus.Object, Options.Create(new NamespaceRepositorySettings()));
        var original = (await repository.Get(ns.Id, ns.OrganizationId)).DefaultDestroyApprovalThreshold;

        try
        {
            var entity = await repository.Get(ns.Id, ns.OrganizationId);
            entity.DefaultDestroyApprovalThreshold = (original ?? 0) + 1;
            await repository.ExecuteUpdate(entity);

            Assert.Single(Published<NamespaceApprovalThresholdModifiedEvent>(bus));
        }
        finally
        {
            await db.Namespaces.Where(n => n.Id == ns.Id).ExecuteUpdateAsync(s => s.SetProperty(n => n.DefaultDestroyApprovalThreshold, original));
        }
    }

    [Fact]
    public async Task An_update_that_leaves_the_threshold_alone_publishes_no_re_evaluation()
    {
        var module = _fixture.Modules["0000"];
        var bus = new Mock<IPublishEndpoint>();
        await using var db = _fixture.CreateDbContext();
        var repository = Modules(db, module.OrganizationId, bus);

        var entity = await repository.Get(module.Id, module.OrganizationId);
        await repository.ExecuteUpdate(entity);

        Assert.Empty(Published<ModuleApprovalThresholdModifiedEvent>(bus));
    }

    [Fact]
    public async Task Moving_a_module_is_checked_against_the_namespace_it_is_moving_to()
    {
        var module = _fixture.Modules["0000"];
        await using var db = _fixture.CreateDbContext();
        var repository = Modules(db, module.OrganizationId, new Mock<IPublishEndpoint>());

        var entity = await repository.Get(module.Id, module.OrganizationId);
        entity.NamespaceId = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidStackReferenceException>(() => repository.ExecuteUpdate(entity));
    }
}
