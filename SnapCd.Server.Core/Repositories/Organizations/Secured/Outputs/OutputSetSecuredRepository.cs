using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.OutputSets;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.Outputs;

public class OutputSetSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<OutputSetRepositorySettings> options,
    QuotaService quotaService)
{
    public OutputSetSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new OutputSetSecuredRepository(
            new OutputSetRepository(dbContext, principalProvider, bus, options, quotaService),
            principalProvider);
    }
}

public class OutputSetSecuredRepository : GenericModuleChildSecuredRepository<
    OutputSet,
    OutputSetReadDto,
    OutputSetRepository,
    OutputSetCreatedEvent,
    OutputSetUpdatedEvent,
    OutputSetDeletedEvent,
    OutputSetRepositorySettings>
{
    public OutputSetSecuredRepository(
        OutputSetRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }


    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader],
        StackRoles = [StackRole.Owner, StackRole.Contributor, StackRole.Reader],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor, NamespaceRole.Reader],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.Reader],
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [],
        StackRoles = [],
        NamespaceRoles = [],
        ModuleRoles = [],
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [],
        StackRoles = [],
        NamespaceRoles = [],
        ModuleRoles = [],
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor],
        ModuleRoles = [ModuleRole.Owner],
    };

    public async Task<OutputSet> Get(Guid moduleId, string checksum, Guid organizationId)
    {
        var entity = await Repository.Get(moduleId, checksum, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to OutputSet {entity.Id}");

        return entity;
    }

    public async Task<OutputSet> GetLatestByModuleId(Guid moduleId, Guid organizationId)
    {
        var entity = await Repository.GetLatestByModuleId(moduleId, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to OutputSet {entity.Id}");

        return entity;
    }

    public async Task<List<OutputSet>> ListSetsByIds(List<Guid> outputSetIds, Guid organizationId)
    {
        var outputSets = await Repository.ListSetsByIds(outputSetIds, organizationId);

        foreach (var outputSet in outputSets)
            if (!CanRead(outputSet.Id, organizationId))
                throw new UnauthorizedAccessException($"Access denied to OutputSet {outputSet.Id}");

        return outputSets;
    }

    public async Task<Guid?> CreateWithOutputs(OutputSet outputSet, Guid organizationId)
    {
        if (!CanCreate(outputSet.ModuleId, organizationId))
            throw new UnauthorizedAccessException($"Access denied to create OutputSet for module {outputSet.ModuleId}");

        return await Repository.CreateWithOutputs(outputSet, organizationId);
    }
}