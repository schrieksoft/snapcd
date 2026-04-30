using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.Variables;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Variables;

public class VariableRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<VariableRepositorySettings> options)
{
    public VariableRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new VariableRepository(dbContext, principalProvider, bus, options);
    }
}

public class VariableRepository : GenericModuleGrandChildRepository<
    Variable,
    VariableSet,
    VariableReadDto,
    InputCreatedEvent,
    InputUpdatedEvent,
    InputDeletedEvent,
    VariableRepositorySettings>
{
    public VariableRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<VariableRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override VariableReadDto MapToDto(Variable entity)
    {
        return VariableMapper.ToDto(entity);
    }

    protected override Func<Variable, Guid> ParentIdAccessor => input => input.VariableSetId;

    protected override Func<SnapCdDbContext, DbSet<VariableSet>> ParentDbSetAccessor => ctx => ctx.VariableSets;

    public Task<List<Variable>> ListByVariableSetIds(List<Guid> variableSetIds, Guid organizationId)
    {
        var inputs = DbContext.Variables
            .Include(x => x.VariableSet)
            .Where(x => variableSetIds.Contains(x.VariableSet.Id) && x.OrganizationId == organizationId)
            .ToList();

        return Task.FromResult(inputs);
    }

    public Task<List<Variable>> ListByIds(List<Guid> inputIds, Guid organizationId)
    {
        var inputs = DbContext.Variables
            .Include(x => x.VariableSet)
            .Where(x => inputIds.Contains(x.Id) && x.OrganizationId == organizationId)
            .ToList();

        return Task.FromResult(inputs);
    }
}