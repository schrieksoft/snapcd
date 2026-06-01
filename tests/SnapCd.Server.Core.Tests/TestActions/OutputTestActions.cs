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

public class OutputTestActions : ITestActions
{
    private readonly Fixture _fixture;
    private readonly SnapCdDbContext _dbContext;

    public OutputTestActions(Fixture fixture, SnapCdDbContext dbContext)
    {
        _fixture = fixture;
        _dbContext = dbContext;
    }

    public async Task CanList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] expectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSecuredRepository(
            new OutputRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputRepositorySettings())),
            principalProvider
        );
        var outputs = await repo.List(_fixture.Organizations["0"].Id);
        foreach (var expectedId in expectedEntityIds) Assert.Contains(outputs, o => o.Id == expectedId);
    }

    public async Task CannotList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] notExpectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSecuredRepository(
            new OutputRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputRepositorySettings())),
            principalProvider
        );
        var outputs = await repo.List(_fixture.Organizations["0"].Id);
        foreach (var notExpectedId in notExpectedEntityIds) Assert.DoesNotContain(outputs, o => o.Id == notExpectedId);
    }

    public async Task CanGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSecuredRepository(
            new OutputRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputRepositorySettings())),
            principalProvider
        );
        var output = await repo.Get(entityId, _fixture.Organizations["0"].Id);
        Assert.NotNull(output);
        Assert.Equal(entityId, output.Id);
    }

    public async Task CannotGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Find which organization this entity belongs to
        var entity = await _dbContext.Outputs.FirstOrDefaultAsync(o => o.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSecuredRepository(
            new OutputRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputRepositorySettings())),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, organizationId));
    }

    public async Task CanUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId, string namePrefix)
    {
        // NOTE: Output cannot be updated by anyone - this should always throw
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSecuredRepository(
            new OutputRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputRepositorySettings())),
            principalProvider
        );
        var output = await repo.Get(entityId, _fixture.Organizations["0"].Id);
        if (output is LiteralOutput literalOutput)
        {
            literalOutput.Value = $"Updated_{namePrefix}";
            await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
                await repo.Update(literalOutput));
        }
    }

    public async Task CannotUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // NOTE: Output cannot be updated by anyone - this should always throw
        var entity = await _dbContext.Outputs.FirstOrDefaultAsync(o => o.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSecuredRepository(
            new OutputRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputRepositorySettings())),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, organizationId));
    }

    public async Task CanDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSecuredRepository(
            new OutputRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputRepositorySettings())),
            principalProvider
        );
        await repo.Delete(entityId, _fixture.Organizations["0"].Id);
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, _fixture.Organizations["0"].Id));
    }

    public async Task CannotDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        // Find which organization this entity belongs to
        var entity = await _dbContext.Outputs.FirstOrDefaultAsync(o => o.Id == entityId);
        var organizationId = entity?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSecuredRepository(
            new OutputRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputRepositorySettings())),
            principalProvider
        );
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Get(entityId, organizationId));
    }

    public async Task CanCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId, string namePrefix)
    {
        // NOTE: Output can only be created by Runner roles, NOT by OrganizationOwner
        // This should always throw for OrganizationOwner
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSecuredRepository(
            new OutputRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputRepositorySettings())),
            principalProvider
        );
        var newOutput = new LiteralOutput
        {
            Id = Guid.NewGuid(),
            Name = $"{namePrefix}_Create_{Guid.NewGuid().ToString("N")[..8]}",
            OutputSetId = parentId,
            OrganizationId = _fixture.Organizations["0"].Id,
            Type = "string",
            Value = "test-value"
        };
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Create(newOutput));
    }

    public async Task CannotCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId)
    {
        // NOTE: Output can only be created by Runner roles, NOT by OrganizationOwner
        // Find which organization this parent belongs to
        var outputSet = await _dbContext.OutputSets.FirstOrDefaultAsync(os => os.Id == parentId);
        var organizationId = outputSet?.OrganizationId ?? _fixture.Organizations["1"].Id;

        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new OutputSecuredRepository(
            new OutputRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new OutputRepositorySettings())),
            principalProvider
        );
        var newOutput = new LiteralOutput
        {
            Id = Guid.NewGuid(),
            Name = $"CannotCreate_{Guid.NewGuid().ToString("N")[..8]}",
            OutputSetId = parentId,
            OrganizationId = organizationId,
            Type = "string",
            Value = "test-value"
        };
        await Assert.ThrowsAsync<PrincipalNotAuthorizedException>(async () =>
            await repo.Create(newOutput));
    }
}