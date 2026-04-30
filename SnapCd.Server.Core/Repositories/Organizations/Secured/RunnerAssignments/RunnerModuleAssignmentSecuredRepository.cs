using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.RunnerModuleAssignments;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerAssignments;

public class RunnerModuleAssignmentSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<RunnerModuleAssignmentRepositorySettings> options)
{
    public RunnerModuleAssignmentSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new RunnerModuleAssignmentSecuredRepository(
            new RunnerModuleAssignmentRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class RunnerModuleAssignmentSecuredRepository : GenericOrganizationChildSecuredRepository<
    RunnerModuleAssignment,
    RunnerModuleAssignmentReadDto,
    RunnerModuleAssignmentRepository,
    RunnerModuleAssignmentCreatedEvent,
    RunnerModuleAssignmentUpdatedEvent,
    RunnerModuleAssignmentDeletedEvent,
    RunnerModuleAssignmentRepositorySettings>
{
    public RunnerModuleAssignmentSecuredRepository(
        RunnerModuleAssignmentRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<List<RunnerModuleAssignment>> ListByRunner(Guid runnerId, Guid organizationId)
    {
        return await Repository.ListByRunner(runnerId, organizationId);
    }

    public async Task<List<RunnerModuleAssignment>> ListByModule(Guid moduleId, Guid organizationId)
    {
        return await Repository.ListByModule(moduleId, organizationId);
    }
}