using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.GroupMembers;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers.GroupMembers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.GroupMembers;

public class ServicePrincipalGroupMemberRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ServicePrincipalGroupMemberRepositorySettings> options)
{
    public ServicePrincipalGroupMemberRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ServicePrincipalGroupMemberRepository(dbContext, principalProvider, bus, options);
    }
}

public class ServicePrincipalGroupMemberRepository : GenericOrganizationChildRepository<ServicePrincipalGroupMember, ServicePrincipalGroupMemberReadDto, ServicePrincipalGroupMemberCreatedEvent,
    ServicePrincipalGroupMemberUpdatedEvent, ServicePrincipalGroupMemberDeletedEvent, ServicePrincipalGroupMemberRepositorySettings>
{
    public ServicePrincipalGroupMemberRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ServicePrincipalGroupMemberRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ServicePrincipalGroupMemberReadDto MapToDto(ServicePrincipalGroupMember entity)
    {
        return ServicePrincipalGroupMemberMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ServicePrincipalGroupMember entity)
    {
        var currentCount = await DbContext.ServicePrincipalGroupMembers
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ServicePrincipalGroupMemberQuota), currentCount);
    }

    public async Task<List<ServicePrincipalGroupMember>> ListByGroupId(Guid groupId, Guid organizationId, IQueryable<ServicePrincipalGroupMember>? query = null)
    {
        query ??= DbContext.Set<ServicePrincipalGroupMember>();

        query = query.Where(gm => gm.OrganizationId == organizationId && gm.GroupId == groupId);

        return await query.ToListAsync();
    }

    public async Task<ServicePrincipalGroupMember> GetByParents(Guid groupId, Guid servicePrincipalId, Guid organizationId)
    {
        var servicePrincipalGroupMember = await DbContext.ServicePrincipalGroupMembers
            .SingleOrDefaultAsync(i => i.GroupId == groupId && i.ServicePrincipalId == servicePrincipalId && i.OrganizationId == organizationId);

        if (servicePrincipalGroupMember == null)
            throw new EntityNotFoundException(
                $"ServicePrincipalGroupMember with GroupId {groupId} and ServicePrincipalId {servicePrincipalId} not found.");

        return servicePrincipalGroupMember;
    }
}