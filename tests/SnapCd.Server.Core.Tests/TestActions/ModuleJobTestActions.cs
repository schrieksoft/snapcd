// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Tests.Infrastructure;

namespace SnapCd.Server.Core.Tests.TestActions;

/// <summary>
/// Test actions for ModuleJob entity - specifically testing CanPostLogs permission.
/// RunnerRunner has custom CanPostLogs permission for jobs belonging to assigned modules.
/// </summary>
public class ModuleJobTestActions : ITestActions
{
    private readonly Fixture _fixture;
    private readonly SnapCdDbContext _dbContext;

    public ModuleJobTestActions(Fixture fixture, SnapCdDbContext dbContext)
    {
        _fixture = fixture;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Tests that principal CAN post logs to specific ModuleJobs (via CanPostLogs method).
    /// </summary>
    public async Task CanList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] expectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new ModuleJobSecuredRepository(
            new ModuleJobRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new ModuleJobRepositorySettings())),
            principalProvider
        );
    }

    /// <summary>
    /// Tests that principal CANNOT post logs to specific ModuleJobs (via CanPostLogs method).
    /// </summary>
    public async Task CannotList(Guid principalId, PrincipalDiscriminator discriminator, Guid[] notExpectedEntityIds)
    {
        var principalProvider = _fixture.CreatePrincipalProvider(principalId, discriminator, _fixture.Organizations["0"].Id);
        var repo = new ModuleJobSecuredRepository(
            new ModuleJobRepository(_dbContext, principalProvider, _fixture.CreateMockBus(), Microsoft.Extensions.Options.Options.Create(new ModuleJobRepositorySettings())),
            principalProvider
        );


    }

    // CanPostLogs is the only permission RunnerRunner has on ModuleJob
    // All other CRUD operations throw NotImplementedException
    public async Task CanGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        throw new NotImplementedException("ModuleJob only supports CanPostLogs testing for RunnerRunner role");
    }

    public async Task CannotGet(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        throw new NotImplementedException("ModuleJob only supports CanPostLogs testing for RunnerRunner role");
    }

    public async Task CanUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId, string namePrefix)
    {
        throw new NotImplementedException("ModuleJob only supports CanPostLogs testing for RunnerRunner role");
    }

    public async Task CannotUpdate(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        throw new NotImplementedException("ModuleJob only supports CanPostLogs testing for RunnerRunner role");
    }

    public async Task CanDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        throw new NotImplementedException("ModuleJob only supports CanPostLogs testing for RunnerRunner role");
    }

    public async Task CannotDelete(Guid principalId, PrincipalDiscriminator discriminator, Guid entityId)
    {
        throw new NotImplementedException("ModuleJob only supports CanPostLogs testing for RunnerRunner role");
    }

    public async Task CanCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId, string namePrefix)
    {
        throw new NotImplementedException("ModuleJob only supports CanPostLogs testing for RunnerRunner role");
    }

    public async Task CannotCreate(Guid principalId, PrincipalDiscriminator discriminator, Guid parentId)
    {
        throw new NotImplementedException("ModuleJob only supports CanPostLogs testing for RunnerRunner role");
    }
}