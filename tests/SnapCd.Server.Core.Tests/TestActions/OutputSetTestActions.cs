// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Outputs;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.TestActions;

public class OutputSetTestActions : ITestActions
{
    private readonly Fixture _fixture;
    private readonly SnapCdDbContext _dbContext;

    public OutputSetTestActions(Fixture fixture, SnapCdDbContext dbContext)
    {
        _fixture = fixture;
        _dbContext = dbContext;
    }

    public async Task CanList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] expectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSetSecuredRepository(
            new OutputSetRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputSetRepositorySettings()), _fixture.CreateMockQuotaService()),
            principalProvider
        );
        var outputSets = await repo.List(_fixture.Organizations["0"].Id);
        foreach (var expectedId in expectedEntityIds) Assert.Contains(outputSets, o => o.Id == expectedId);
    }

    public async Task CannotList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] notExpectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSetSecuredRepository(
            new OutputSetRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputSetRepositorySettings()), _fixture.CreateMockQuotaService()),
            principalProvider
        );
        var outputSets = await repo.List(_fixture.Organizations["0"].Id);
        foreach (var notExpectedId in notExpectedEntityIds) Assert.DoesNotContain(outputSets, o => o.Id == notExpectedId);
    }

    public async Task CanGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSetSecuredRepository(
            new OutputSetRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputSetRepositorySettings()), _fixture.CreateMockQuotaService()),
            principalProvider
        );
        var outputSet = await repo.Get(entityId, _fixture.Organizations["0"].Id);
        Assert.NotNull(outputSet);
        Assert.Equal(entityId, outputSet.Id);
    }

    public async Task CannotGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Find which organization this entity belongs to
        var entity = await _dbContext.OutputSets.FirstOrDefaultAsync(o => o.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSetSecuredRepository(
            new OutputSetRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputSetRepositorySettings()), _fixture.CreateMockQuotaService()),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, organizationId));
    }

    public async Task CanUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId, string namePrefix)
    {
        // NOTE: OutputSet cannot be updated by anyone - this should always throw
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSetSecuredRepository(
            new OutputSetRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputSetRepositorySettings()), _fixture.CreateMockQuotaService()),
            principalProvider
        );
        var outputSet = await repo.Get(entityId, _fixture.Organizations["0"].Id);
        outputSet.Checksum = $"Updated_{namePrefix}";
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Update(outputSet));
    }

    public async Task CannotUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // NOTE: OutputSet cannot be updated by anyone - this should always throw
        var entity = await _dbContext.OutputSets.FirstOrDefaultAsync(o => o.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSetSecuredRepository(
            new OutputSetRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputSetRepositorySettings()), _fixture.CreateMockQuotaService()),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, organizationId));
    }

    public async Task CanDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSetSecuredRepository(
            new OutputSetRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputSetRepositorySettings()), _fixture.CreateMockQuotaService()),
            principalProvider
        );
        await repo.Delete(entityId, _fixture.Organizations["0"].Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, _fixture.Organizations["0"].Id));
    }

    public async Task CannotDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Find which organization this entity belongs to
        var entity = await _dbContext.OutputSets.FirstOrDefaultAsync(o => o.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSetSecuredRepository(
            new OutputSetRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputSetRepositorySettings()), _fixture.CreateMockQuotaService()),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Delete(entityId, organizationId));
    }

    public async Task CanCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId, string namePrefix)
    {
        // For OutputSet, parentId is a ModuleId
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSetSecuredRepository(
            new OutputSetRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputSetRepositorySettings()), _fixture.CreateMockQuotaService()),
            principalProvider
        );
        var newOutputSet = new OutputSet
        {
            Id = Guid.NewGuid(),
            ModuleId = parentId,
            OrganizationId = _fixture.Organizations["0"].Id,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Checksum = $"{namePrefix}_Create_{Guid.NewGuid().ToString("N")[..8]}"
        };
        var created = await repo.Create(newOutputSet);
        Assert.NotNull(created);
        Assert.Equal(newOutputSet.Checksum, created.Checksum);
    }

    public async Task CannotCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId)
    {
        // For OutputSet, parentId is a ModuleId
        var module = await _dbContext.Modules.FirstOrDefaultAsync(m => m.Id == parentId);
        var organizationId = module?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSetSecuredRepository(
            new OutputSetRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputSetRepositorySettings()), _fixture.CreateMockQuotaService()),
            principalProvider
        );
        var newOutputSet = new OutputSet
        {
            Id = Guid.NewGuid(),
            ModuleId = parentId,
            OrganizationId = organizationId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Checksum = $"CannotCreate_{Guid.NewGuid().ToString("N")[..8]}"
        };
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Create(newOutputSet));
    }
}