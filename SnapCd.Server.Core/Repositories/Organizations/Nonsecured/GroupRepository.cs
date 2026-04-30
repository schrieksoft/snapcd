using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.Groups;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class GroupRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<GroupRepositorySettings> options)
{
    public GroupRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new GroupRepository(dbContext, principalProvider, bus, options);
    }
}

public class GroupRepository : GenericOrganizationChildRepository<Group, GroupReadDto, GroupCreatedEvent, GroupUpdatedEvent, GroupDeletedEvent, GroupRepositorySettings>
{
    public GroupRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<GroupRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override GroupReadDto MapToDto(Group entity)
    {
        return GroupMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(Group entity)
    {
        var currentCount = await DbContext.Groups
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.GroupQuota), currentCount);
    }

    public async Task<Group?> GetByName(string name, Guid organizationId)
    {
        return await DbContext.Groups
            .Where(g => g.OrganizationId == organizationId)
            .SingleOrDefaultAsync(g => g.Name == name);
    }
}