// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Custom.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Repositories.Custom.Secured;

public class ModuleSagaSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    ModuleSecuredRepositoryFactory moduleSecuredRepositoryFactory,
    IBus bus)
{
    public ModuleSagaSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var sagaRepositoryFactory = new ModuleSagaRepositoryFactory(dbFactory, bus);
        var sagaRepository = sagaRepositoryFactory.Create();
        var moduleSecuredRepository = moduleSecuredRepositoryFactory.Create(principalProvider);
        return new ModuleSagaSecuredRepository(sagaRepository, moduleSecuredRepository);
    }
}

public class ModuleSagaSecuredRepository : IDisposable
{
    protected readonly ModuleSagaRepository Repository;
    protected readonly ModuleSecuredRepository ModuleSecuredRepository;

    public ModuleSagaSecuredRepository(
        ModuleSagaRepository repository,
        ModuleSecuredRepository moduleSecuredRepository)
    {
        Repository = repository;
        ModuleSecuredRepository = moduleSecuredRepository;
    }

    public virtual async Task<ModuleSaga> Get(Guid correlationId, Guid organizationId)
    {
        if (ModuleSecuredRepository.CanRead(correlationId, organizationId))
            return await Repository.Get(correlationId, organizationId);
        else
            throw new PrincipalNotAuthorizedException(
                $"Module with ID {correlationId} not found or {ModuleSecuredRepository.PrincipalDiscriminator} with ID {ModuleSecuredRepository.PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");
    }

    public virtual async Task<ModuleSaga> SetPaused(
        Guid correlationId,
        Guid organizationId,
        bool paused,
        string? reason)
    {
        if (!ModuleSecuredRepository.CanPause(correlationId, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"Module with ID {correlationId} not found or {ModuleSecuredRepository.PrincipalDiscriminator} with ID {ModuleSecuredRepository.PrincipalProvider.GetSubject(organizationId)} does not have permission to pause it.");

        var principalId = ModuleSecuredRepository.PrincipalProvider.GetSubject(organizationId);
        return await Repository.SetPaused(correlationId, organizationId, paused, principalId, reason);
    }

    public void Dispose()
    {
        Repository?.Dispose();
        ModuleSecuredRepository?.Dispose();
    }
}