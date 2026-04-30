using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services;

public class SourceChangedServiceFactory
{
    private readonly IBus _bus;
    private readonly IDbContextFactory<SnapCdDbContext> _dbFactory;

    public SourceChangedServiceFactory(
        IBus bus,
        IDbContextFactory<SnapCdDbContext> dbFactory
    )
    {
        _bus = bus;
        _dbFactory = dbFactory;
    }

    public SourceChangedService Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = _dbFactory.CreateDbContext();

        return new SourceChangedService(_bus, dbContext, principalProvider);
    }
}

public class SourceChangedService
{
    private readonly IBus _bus;
    private readonly SnapCdDbContext _dbContext;
    private readonly IPrincipalProvider _principalProvider;

    public SourceChangedService(
        IBus bus,
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider
    )
    {
        _bus = bus;
        _dbContext = dbContext;
        _principalProvider = principalProvider;
    }

    public bool CanNotify(PrincipalDiscriminator principalDiscriminator, Guid principalId, List<OrganizationRole> validRoles, Guid organizationId)
    {
        return principalDiscriminator switch
        {
            PrincipalDiscriminator.User => _dbContext.UserOrganizationRoleAssignments
                .Any(x =>
                    x.OrganizationId == organizationId &&
                    x.PrincipalId == principalId &&
                    validRoles.Contains(x.RoleName)),
            PrincipalDiscriminator.ServicePrincipal => _dbContext.ServicePrincipalOrganizationRoleAssignments
                .Any(x =>
                    x.OrganizationId == organizationId &&
                    x.PrincipalId == principalId &&
                    validRoles.Contains(x.RoleName)),
            _ => false
        };
    }

    public async Task NotifyChange(SourceChangedDto dto, Guid organizationId)
    {
        var validRoles = new List<OrganizationRole> { OrganizationRole.SourceChangeNotifier, OrganizationRole.Owner, OrganizationRole.Contributor };
        if (!CanNotify(_principalProvider.GetPrincipalDiscriminator(), _principalProvider.GetSubject(organizationId), validRoles, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{_principalProvider.GetPrincipalDiscriminator()} with ID {_principalProvider.GetSubject(organizationId)} does not have permission to notify source changes");

        var moduleIds = _dbContext.Modules
            .Where(x =>
                x.SourceUrl == dto.SourceUrl &&
                x.SourceRevision == dto.SourceRevision &&
                x.SourceType == dto.SourceType &&
                x.TriggerOnSourceChangedNotification &&
                x.OrganizationId == organizationId
            )
            .Select(x => x.Id)
            .ToList();

        foreach (var moduleId in moduleIds)
            await _bus.Publish(new GatekeepingJobRequested
            {
                ModuleId = moduleId,
                OrganizationId = organizationId,
                DesiredStateHeadline = DesiredStateHeadline.Applied,
                SetNewDesiredState = false
            }, publishContext => { publishContext.TimeToLive = TimeSpan.FromMinutes(5); });
    }
}