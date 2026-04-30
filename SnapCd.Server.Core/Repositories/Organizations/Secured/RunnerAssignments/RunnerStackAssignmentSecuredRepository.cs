using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.RunnerStackAssignments;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerAssignments;

public class RunnerStackAssignmentSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<RunnerStackAssignmentRepositorySettings> options)
{
    public RunnerStackAssignmentSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new RunnerStackAssignmentSecuredRepository(
            new RunnerStackAssignmentRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class RunnerStackAssignmentSecuredRepository : GenericOrganizationChildSecuredRepository<
    RunnerStackAssignment,
    RunnerStackAssignmentReadDto,
    RunnerStackAssignmentRepository,
    RunnerStackAssignmentCreatedEvent,
    RunnerStackAssignmentUpdatedEvent,
    RunnerStackAssignmentDeletedEvent,
    RunnerStackAssignmentRepositorySettings>
{
    public RunnerStackAssignmentSecuredRepository(
        RunnerStackAssignmentRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<List<RunnerStackAssignment>> ListByRunner(Guid runnerId, Guid organizationId)
    {
        return await Repository.ListByRunner(runnerId, organizationId);
    }

    public async Task<List<RunnerStackAssignment>> ListByStack(Guid stackId, Guid organizationId)
    {
        return await Repository.ListByStack(stackId, organizationId);
    }
}