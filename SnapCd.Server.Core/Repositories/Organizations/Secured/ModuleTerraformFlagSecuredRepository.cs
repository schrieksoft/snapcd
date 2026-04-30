using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModuleTerraformFlags;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ModuleTerraformFlagSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModuleTerraformFlagRepositorySettings> options)
{
    public ModuleTerraformFlagSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleTerraformFlagSecuredRepository(
            new ModuleTerraformFlagRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ModuleTerraformFlagSecuredRepository : GenericModuleChildSecuredRepository<
    ModuleTerraformFlag,
    ModuleTerraformFlagReadDto,
    ModuleTerraformFlagRepository,
    ModuleTerraformFlagCreatedEvent,
    ModuleTerraformFlagUpdatedEvent,
    ModuleTerraformFlagDeletedEvent,
    ModuleTerraformFlagRepositorySettings>
{
    public ModuleTerraformFlagSecuredRepository(
        ModuleTerraformFlagRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }
}
