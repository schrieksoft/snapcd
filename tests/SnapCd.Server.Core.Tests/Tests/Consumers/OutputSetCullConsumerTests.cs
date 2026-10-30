// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Server.Core.Consumers.System.Competing;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;
using SnapCd.Server.Core.Settings;
using SnapCd.Server.Core.Services.Utils.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;
using Xunit;

namespace SnapCd.Server.Core.Tests.Tests.Consumers;

[Collection("NewRoleBasedSharedFixture")]
public class OutputSetCullConsumerTests
{
    private readonly Fixture _fixture;

    public OutputSetCullConsumerTests(Fixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Culling_off_keeps_every_set()
    {
        var sets = await WithSets(4, async module =>
            await Consume(module, new SetRetentionSettings
            {
                OutputSets = new OutputSetRetentionSettings { CullAboveLimit = false, MaxKeptPerModule = 2 }
            }));

        Assert.Equal(4, sets.Count);
    }

    [Fact]
    public async Task Culling_on_keeps_the_newest_up_to_the_limit()
    {
        var sets = await WithSets(4, async module =>
            await Consume(module, new SetRetentionSettings
            {
                OutputSets = new OutputSetRetentionSettings { CullAboveLimit = true, MaxKeptPerModule = 2 }
            }));

        Assert.Equal(2, sets.Count);
        Assert.Equal(["set-3", "set-2"], sets.Select(s => s.Checksum).ToList());
    }

    [Fact]
    public async Task A_module_under_the_limit_loses_nothing()
    {
        var sets = await WithSets(2, async module =>
            await Consume(module, new SetRetentionSettings
            {
                OutputSets = new OutputSetRetentionSettings { CullAboveLimit = true, MaxKeptPerModule = 5 }
            }));

        Assert.Equal(2, sets.Count);
    }

    [Fact]
    public async Task A_limit_below_one_culls_nothing()
    {
        var sets = await WithSets(3, async module =>
            await Consume(module, new SetRetentionSettings
            {
                OutputSets = new OutputSetRetentionSettings { CullAboveLimit = true, MaxKeptPerModule = 0 }
            }));

        Assert.Equal(3, sets.Count);
    }

    [Fact]
    public async Task A_delete_event_is_raised_for_each_culled_set()
    {
        var published = new List<OutputSetDeletedEvent>();

        await WithSets(4, async module =>
            await Consume(module, new SetRetentionSettings
            {
                OutputSets = new OutputSetRetentionSettings { CullAboveLimit = true, MaxKeptPerModule = 1 }
            }, published));

        // The event is what reclaims the set's sensitive outputs from the vault.
        Assert.Equal(3, published.Count);
        Assert.All(published, e => Assert.Equal(_fixture.Modules["0000"].Id, e.Data.ModuleId));
    }

    /// <summary>
    /// Gives the Module its own sets, runs the consumer, and returns what survived newest first.
    /// The sets are removed afterwards whatever happened, so the shared fixture is left as it was.
    /// </summary>
    private async Task<List<OutputSet>> WithSets(int count, Func<Module, Task> act)
    {
        var module = _fixture.Modules["0000"];

        await using var db = _fixture.CreateDbContext();

        var existing = await db.OutputSets
            .Where(s => s.ModuleId == module.Id && s.OrganizationId == module.OrganizationId)
            .Select(s => s.Id)
            .ToListAsync();

        var added = new List<OutputSet>();
        for (var i = 0; i < count; i++)
            added.Add(new OutputSet
            {
                Id = Guid.NewGuid(),
                ModuleId = module.Id,
                OrganizationId = module.OrganizationId,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + i,
                Checksum = $"set-{i}"
            });

        db.OutputSets.AddRange(added);
        await db.SaveChangesAsync();

        try
        {
            await act(module);

            await using var read = _fixture.CreateDbContext();
            return await read.OutputSets
                .Where(s => s.ModuleId == module.Id &&
                            s.OrganizationId == module.OrganizationId &&
                            !existing.Contains(s.Id))
                .OrderByDescending(s => s.Timestamp)
                .ToListAsync();
        }
        finally
        {
            await using var cleanup = _fixture.CreateDbContext();
            var leftover = await cleanup.OutputSets
                .Where(s => s.ModuleId == module.Id &&
                            s.OrganizationId == module.OrganizationId &&
                            !existing.Contains(s.Id))
                .ToListAsync();
            cleanup.OutputSets.RemoveRange(leftover);
            await cleanup.SaveChangesAsync();
        }
    }

    private async Task Consume(
        Module module,
        SetRetentionSettings settings,
        List<OutputSetDeletedEvent>? published = null)
    {
        var bus = new Mock<IPublishEndpoint>();
        bus.Setup(b => b.Publish(
                It.IsAny<OutputSetDeletedEvent>(),
                It.IsAny<IPipe<PublishContext<OutputSetDeletedEvent>>>(),
                It.IsAny<CancellationToken>()))
            .Callback<OutputSetDeletedEvent, IPipe<PublishContext<OutputSetDeletedEvent>>, CancellationToken>(
                (m, _, _) => published?.Add(m))
            .Returns(Task.CompletedTask);

        var principalProvider = new LiteralPrincipalProvider(
            Guid.Empty, PrincipalDiscriminator.User, [module.OrganizationId]);

        await using var consumerDb = _fixture.CreateDbContext();
        await using var repositoryDb = _fixture.CreateDbContext();

        var repository = new OutputSetRepository(
            repositoryDb,
            principalProvider,
            bus.Object,
            Options.Create(new OutputSetRepositorySettings()),
            _fixture.CreateMockQuotaService());

        var context = new Mock<ConsumeContext<OutputSetWithOutputsCreatedEvent>>();
        context.Setup(c => c.Message).Returns(new OutputSetWithOutputsCreatedEvent
        {
            Data = new OutputSetWithOutputsDto
            {
                Id = Guid.NewGuid(),
                ModuleId = module.Id,
                CreatedOrUpdatedOutputs = []
            },
            OrganizationId = module.OrganizationId
        });

        await new OutputSetCullConsumer(
            consumerDb,
            repository,
            Options.Create(settings),
            NullLogger<OutputSetCullConsumer>.Instance).Consume(context.Object);
    }
}
