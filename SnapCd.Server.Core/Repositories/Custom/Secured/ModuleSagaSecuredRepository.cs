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
    ModuleSecuredRepositoryFactory moduleSecuredRepositoryFactory)
{
    public ModuleSagaSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var sagaRepositoryFactory = new ModuleSagaRepositoryFactory(dbFactory);
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

    public void Dispose()
    {
        Repository?.Dispose();
        ModuleSecuredRepository?.Dispose();
    }
}