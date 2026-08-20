// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ManualModuleJobSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    ModuleSecuredRepositoryFactory moduleSecuredRepositoryFactory)
{
    public ManualModuleJobSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        principalProvider ??= new HttpContextPrincipalProvider(new HttpContextAccessor());

        return new ManualModuleJobSecuredRepository(
            new ManualModuleJobRepository(dbFactory.CreateDbContext()),
            moduleSecuredRepositoryFactory.Create(principalProvider));
    }
}

/// <summary>
/// Reading a Module's manual jobs is reading the Module: the jobs carry no permissions of their
/// own, so access is delegated rather than duplicated.
/// </summary>
public class ManualModuleJobSecuredRepository : IDisposable
{
    private readonly ManualModuleJobRepository _repository;
    private readonly ModuleSecuredRepository _moduleSecuredRepository;

    public ManualModuleJobSecuredRepository(
        ManualModuleJobRepository repository,
        ModuleSecuredRepository moduleSecuredRepository)
    {
        _repository = repository;
        _moduleSecuredRepository = moduleSecuredRepository;
    }

    public async Task<List<ManualModuleJob>> ListByModule(Guid moduleId, Guid organizationId, int take = 50)
    {
        if (!_moduleSecuredRepository.CanRead(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Module with ID {moduleId} not found or the principal does not have permission to read it.");

        return await _repository.ListByModule(moduleId, organizationId, take);
    }

    public async Task<ManualModuleJob> Get(Guid id, Guid moduleId, Guid organizationId)
    {
        if (!_moduleSecuredRepository.CanRead(moduleId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Module with ID {moduleId} not found or the principal does not have permission to read it.");

        return await _repository.Get(id, organizationId);
    }

    public void Dispose()
    {
        _repository?.Dispose();
        _moduleSecuredRepository?.Dispose();
    }
}
